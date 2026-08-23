#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
codexctl — RESIDUUM 项目的 Codex CLI 中转控制器

作用：把 "Codex 写代码 → 自审 → 编译验证 → Claude 终审" 这条链
      从手动变成一条命令，并且把 Codex 唯一的能力缺口（云端/CLI 里
      编译不了 C#）用本地 Unity batchmode 补上。

设计原则：
  1. 零第三方依赖，Python 3.9+（macOS 自带 python3 即可跑）
  2. 一切决定性检查（契约文件保护、Unity 6 API 铁律）用正则本地做，
     不信任 LLM 的自我陈述
  3. Codex 的自审用 --output-schema 强制成结构化 JSON，可机读、可比对
  4. 全过程留痕到 .codexctl/runs/，Claude 终审时只读这个目录即可

参考：codex exec 非交互模式（--json / --output-schema / resume）
"""

from __future__ import annotations

import argparse
import datetime as _dt
import glob
import json
import os
import re
import shlex
import shutil
import subprocess
import sys
import textwrap

VERSION = "0.1.0"

# ---------------------------------------------------------------- 终端输出

class C:
    R = "\033[31m"; G = "\033[32m"; Y = "\033[33m"; B = "\033[34m"
    DIM = "\033[2m"; BOLD = "\033[1m"; X = "\033[0m"
    @staticmethod
    def off():
        for k in ("R", "G", "Y", "B", "DIM", "BOLD", "X"):
            setattr(C, k, "")

if not sys.stdout.isatty() or os.environ.get("NO_COLOR"):
    C.off()

def info(msg):  print("%s▸%s %s" % (C.B, C.X, msg))
def ok(msg):    print("%s✓%s %s" % (C.G, C.X, msg))
def warn(msg):  print("%s!%s %s" % (C.Y, C.X, msg))
def fail(msg):  print("%s✗%s %s" % (C.R, C.X, msg))
def step(msg):  print("\n%s%s%s" % (C.BOLD, msg, C.X))
def dim(msg):   print("%s%s%s" % (C.DIM, msg, C.X))

def die(msg, code=1):
    fail(msg)
    sys.exit(code)

def now_stamp():
    return _dt.datetime.now().strftime("%Y%m%d-%H%M%S")

def expand(p):
    return os.path.abspath(os.path.expanduser(os.path.expandvars(p)))

# ---------------------------------------------------------------- 配置

DEFAULT_CONFIG = {
    "project_root": "~/RESIDUUM",
    "codex": {
        "bin": "codex",
        "model": None,
        "sandbox": "workspace-write",
        "approval": "never",
        "extra_args": [],
        "timeout_sec": 5400,
        "use_subscription_auth": True,
        "env": {},
    },
    "unity": {
        "enabled": True,
        "editor_path": None,
        "hub_roots": ["/Applications/Unity/Hub/Editor", "~/Applications/Unity/Hub/Editor"],
        "extra_args": [],
        "timeout_sec": 2400,
        "project_scope_prefix": "Assets/_Project",
    },
    "git": {
        "branch_prefix": "codex/",
        "auto_branch": True,
        "auto_commit": False,
        "base_branch": "main",
    },
    "repair": {"max_attempts": 2},
    "contract_files": [
        "Assets/_Project/Scripts/Core/GameEvents.cs",
        "Assets/_Project/Scripts/Core/RoundResult.cs",
        "Assets/_Project/Scripts/Evidence/EvidenceType.cs",
        "Assets/_Project/Scripts/Evidence/IEvidenceSource.cs",
        "Assets/_Project/Scripts/World/IInteractable.cs",
        "Assets/_Project/Scripts/Items/IHoldable.cs",
        "Assets/_Project/Scripts/Ghost/GhostDefinition.cs",
    ],
    "gate_rules": [
        {"id": "G01", "severity": "error",
         "pattern": r"\bFindObjectsOfType\b|\bFindObjectOfType\b",
         "message": "Unity 6 已弃用 FindObjectOfType，改用 FindAnyObjectByType / FindObjectsByType"},
        # Unity 6000.5 又把 FindFirstObjectByType 也标了弃用（它依赖 instance ID 排序）。
        # 单列一条 warn 而不是并进 G01，是为了让「早已弃用」和「刚弃用」在报告里能分开看。
        {"id": "G09", "severity": "warn",
         "pattern": r"\bFindFirstObjectByType\b",
         "message": "FindFirstObjectByType 在 Unity 6000.5 已弃用（依赖 instance ID 排序），改用 FindAnyObjectByType"},
        {"id": "G10", "severity": "warn",
         "pattern": r"\bFindObjectsSortMode\b",
         "message": "带 FindObjectsSortMode 的 FindObjectsByType 重载在 Unity 6000.5 已弃用，改用不带该参数的重载"},
        {"id": "G02", "severity": "error",
         "pattern": r"\b(?:rb|rigidbody|_rb|_rigidbody|Rigidbody)\s*\.\s*velocity\b",
         "message": "Rigidbody.velocity 在 Unity 6 已改名 linearVelocity"},
        {"id": "G03", "severity": "error",
         "pattern": r"\bUnityEngine\.Input\.|(?<![\w.])Input\s*\.\s*(?:GetKey|GetAxis|GetButton|GetMouse|mousePosition)",
         "message": "禁止旧版 Input Manager，本项目统一走 Input System"},
        {"id": "G04", "severity": "error",
         "pattern": r"\bCinemachineVirtualCamera\b",
         "message": "Cinemachine 3.x 已改名 CinemachineCamera"},
        {"id": "G05", "severity": "error",
         "pattern": r"^\s*using\s+Residuum\.(?!Core\b)",
         "message": "模块之间禁止直接 using，跨模块通信一律走 Core.GameEvents 事件总线"},
        {"id": "G06", "severity": "warn",
         # =(?!>) 是为了放过 `public float Foo => ...` 这类只读表达式体属性，
         # 它们不是可变的 public 字段，早先的 (?:=|;) 会把它们全部误报。
         "pattern": r"^\s*public\s+(?:float|int|bool|string|Vector[23])\s+\w+\s*(?:=(?!>)|;)",
         "message": "可调数值应当 [SerializeField] private，而非 public 字段"},
        {"id": "G07", "severity": "warn",
         "pattern": r"\bGameObject\.Find\s*\(",
         "message": "GameObject.Find 按名字查找脆弱，改用序列化引用"},
        {"id": "G08", "severity": "warn",
         "pattern": r"\bDebug\.Log\s*\(",
         "message": "提交前清理调试日志（或改用条件编译）"},
    ],
    "gate_include": ["Assets/_Project/Scripts/**/*.cs"],
    "audit_checklist": [
        {"id": "A01", "text": "只 using Residuum.Core，没有跨模块直接引用"},
        {"id": "A02", "text": "跨模块通信全部走 GameEvents 静态事件总线"},
        {"id": "A03", "text": "没有修改任何契约文件"},
        {"id": "A04", "text": "零 FindObjectOfType / GameObject.Find"},
        {"id": "A05", "text": "零 Rigidbody.velocity（用 linearVelocity）"},
        {"id": "A06", "text": "零旧版 Input（UnityEngine.Input.*）"},
        {"id": "A07", "text": "零 CinemachineVirtualCamera（用 CinemachineCamera）"},
        {"id": "A08", "text": "所有可调数值 [SerializeField] + 中文 Tooltip"},
        {"id": "A09", "text": "OnEnable / OnDisable 事件订阅与退订成对"},
        {"id": "A10", "text": "OnDestroy 清理所有引用与协程"},
        {"id": "A11", "text": "静态事件在回合开始时重置，不跨局残留"},
        {"id": "A12", "text": "帧率无关：所有插值用 dt，不用固定系数"},
        {"id": "A13", "text": "没有在 Update 里做 GetComponent / 分配内存"},
        {"id": "A14", "text": "空引用防护：外部序列化引用为空时不炸"},
        {"id": "A15", "text": "命名空间与目录结构一致"},
        {"id": "A16", "text": "没有引入新的第三方包或 Unity 包依赖"},
        {"id": "A17", "text": "没有留下 TODO / 未实现的分支"},
        {"id": "A18", "text": "符合任务验收标准里逐条列出的行为"},
    ],
}

def deep_merge(base, over):
    out = dict(base)
    for k, v in (over or {}).items():
        if isinstance(v, dict) and isinstance(out.get(k), dict):
            out[k] = deep_merge(out[k], v)
        else:
            out[k] = v
    return out

def load_config(path=None):
    cfg = json.loads(json.dumps(DEFAULT_CONFIG))
    candidates = [path] if path else [
        os.path.join(os.getcwd(), "codexctl.config.json"),
        os.path.join(os.path.dirname(expand(__file__)), "codexctl.config.json"),
        expand("~/.config/codexctl/config.json"),
    ]
    used = None
    for c in candidates:
        if c and os.path.isfile(expand(c)):
            with open(expand(c), "r", encoding="utf-8") as f:
                cfg = deep_merge(cfg, json.load(f))
            used = expand(c)
            break
    if path and not used:
        die("找不到配置文件：%s" % path)
    cfg["_config_path"] = used
    cfg["project_root"] = expand(cfg["project_root"])
    return cfg

# ---------------------------------------------------------------- 任务包

class Task(object):
    def __init__(self, meta, body, path):
        self.id = meta.get("id") or os.path.splitext(os.path.basename(path))[0]
        self.title = meta.get("title", self.id)
        self.branch = meta.get("branch") or ""
        self.depends = [x.strip() for x in meta.get("depends", "").split(",") if x.strip()]
        self.unity = str(meta.get("unity", "true")).lower() not in ("false", "0", "no")
        self.body = body.strip()
        self.path = path
        self.meta = meta

    def __repr__(self):
        return "<Task %s %s>" % (self.id, self.title)

FRONTMATTER_RE = re.compile(r"\A---\s*\n(.*?)\n---\s*\n", re.S)

def parse_task_file(path):
    with open(path, "r", encoding="utf-8") as f:
        raw = f.read()
    m = FRONTMATTER_RE.match(raw)
    meta, body = {}, raw
    if m:
        for line in m.group(1).splitlines():
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            if ":" not in line:
                continue
            k, v = line.split(":", 1)
            meta[k.strip()] = v.strip()
        body = raw[m.end():]
    return Task(meta, body, path)

def tasks_dir(cfg):
    base = os.path.dirname(cfg.get("_config_path") or expand(__file__))
    return os.path.join(base, "tasks")

def load_tasks(cfg):
    out = {}
    d = tasks_dir(cfg)
    if not os.path.isdir(d):
        return out
    for p in sorted(glob.glob(os.path.join(d, "*.md"))):
        if os.path.basename(p).startswith("_"):
            continue
        t = parse_task_file(p)
        out[t.id.upper()] = t
    return out

def get_task(cfg, tid):
    tasks = load_tasks(cfg)
    t = tasks.get(tid.upper())
    if not t:
        die("任务包里没有 %s（tasks/ 目录：%s）" % (tid, tasks_dir(cfg)))
    return t

def slugify(s):
    s = re.sub(r"[^\w一-鿿]+", "-", s).strip("-").lower()
    return re.sub(r"-{2,}", "-", s) or "task"

def branch_for(cfg, task):
    if task.branch:
        return task.branch
    return "%s%s-%s" % (cfg["git"]["branch_prefix"], task.id.lower(), slugify(task.title))

# ---------------------------------------------------------------- 运行目录

def run_dir(cfg, task_id, stamp=None):
    stamp = stamp or now_stamp()
    d = os.path.join(cfg["project_root"], ".codexctl", "runs", task_id.upper(), stamp)
    os.makedirs(d, exist_ok=True)
    return d

def latest_run_dir(cfg, task_id):
    base = os.path.join(cfg["project_root"], ".codexctl", "runs", task_id.upper())
    if not os.path.isdir(base):
        return None
    subs = sorted(d for d in os.listdir(base) if os.path.isdir(os.path.join(base, d)))
    return os.path.join(base, subs[-1]) if subs else None

def write(path, text):
    with open(path, "w", encoding="utf-8") as f:
        f.write(text)
    return path

def read_if(path):
    try:
        with open(path, "r", encoding="utf-8", errors="replace") as f:
            return f.read()
    except Exception:
        return ""

# ---------------------------------------------------------------- git

def git(cfg, *args, **kw):
    check = kw.pop("check", False)
    p = subprocess.run(["git", "-C", cfg["project_root"]] + list(args),
                       capture_output=True, text=True)
    if check and p.returncode != 0:
        die("git %s 失败：%s" % (" ".join(args), p.stderr.strip()))
    return p

def git_out(cfg, *args):
    return git(cfg, *args).stdout.strip()

def is_git_repo(cfg):
    return git(cfg, "rev-parse", "--git-dir").returncode == 0

def working_tree_dirty(cfg):
    return dirty_list(cfg) != ""


def dirty_list(cfg):
    lines = []
    for ln in git_out(cfg, *(["status", "--porcelain"] + PATHSPEC)).splitlines():
        rel = ln[3:].strip().strip('"')
        if not _excluded(rel):
            lines.append(ln)
    return "\n".join(lines)

def current_branch(cfg):
    return git_out(cfg, "rev-parse", "--abbrev-ref", "HEAD")

# Unity 工程里这些目录是生成物，永远不参与 diff / 闸门
EXCLUDE = ["Library", "Temp", "Logs", "obj", "Build", "Builds", "UserSettings", ".codexctl"]
PATHSPEC = ["--"] + [":(exclude)%s/**" % d for d in EXCLUDE]


def _excluded(rel):
    head = rel.replace("\\", "/").split("/", 1)[0]
    return head in EXCLUDE


def stage_intent(cfg):
    """git add -N：让新建的未跟踪文件也出现在 diff 里（不真正暂存内容）。"""
    git(cfg, *(["add", "-N"] + PATHSPEC + ["."]))


def changed_files(cfg, since_ref):
    """相对某个 ref 的改动文件（含未跟踪）。"""
    stage_intent(cfg)
    tracked = git_out(cfg, *(["diff", "--name-only", since_ref] + PATHSPEC)).splitlines()
    untracked = git_out(cfg, "ls-files", "--others", "--exclude-standard").splitlines()
    seen, out = set(), []
    for f in tracked + untracked:
        f = f.strip()
        if f and f not in seen and not _excluded(f):
            seen.add(f); out.append(f)
    return out

def diffstat(cfg, since_ref):
    stage_intent(cfg)
    return git_out(cfg, *(["diff", "--stat", since_ref] + PATHSPEC))

def full_diff(cfg, since_ref, max_bytes=400000):
    stage_intent(cfg)
    d = git_out(cfg, *(["diff", since_ref] + PATHSPEC))
    if len(d) > max_bytes:
        d = d[:max_bytes] + "\n\n... [diff 过大已截断] ...\n"
    return d

# ---------------------------------------------------------------- codex 驱动

class CodexResult(object):
    def __init__(self):
        self.thread_id = None
        self.final_message = ""
        self.usage = {}
        self.returncode = -1
        self.events = []
        self.jsonl_path = None
        self.stderr_path = None

def codex_env(cfg):
    """默认屏蔽 OPENAI_API_KEY，强制走 ChatGPT 订阅额度而不是 API 计费。
    要用 API key 就在 config 里把 use_subscription_auth 设 false。"""
    env = dict(os.environ)
    if cfg["codex"].get("use_subscription_auth", True):
        env.pop("OPENAI_API_KEY", None)
    env.update({k: str(v) for k, v in (cfg["codex"].get("env") or {}).items()})
    return env


def build_codex_cmd(cfg, prompt, resume_thread=None, output_schema=None,
                    output_last=None, cwd=None):
    """注意：
    1. -a/--ask-for-approval 是全局标志，必须放在 exec 子命令之前。
    2. `codex exec resume <id>` 子命令不接受 --sandbox / -C ——
       resume 会沿用被恢复会话原本的沙箱和工作目录，这两个参数只有
       在开新会话（不 resume）时才能传。传了会直接报参数错误。
    """
    cc = cfg["codex"]
    cmd = [cc["bin"]]
    if cc.get("approval"):
        cmd += ["-a", cc["approval"]]
    cmd += ["exec"]
    if resume_thread:
        cmd += ["resume", resume_thread]
    cmd += ["--json"]
    if not resume_thread:
        if cc.get("sandbox"):
            cmd += ["--sandbox", cc["sandbox"]]
        if cwd:
            cmd += ["-C", cwd]
    if cc.get("model"):
        cmd += ["-m", cc["model"]]
    if output_schema:
        cmd += ["--output-schema", output_schema]
    if output_last:
        cmd += ["-o", output_last]
    cmd += list(cc.get("extra_args") or [])
    cmd += [prompt]          # 位置参数优先级高于 stdin，必须放最后
    return cmd

def run_codex(cfg, prompt, out_dir, tag, resume_thread=None,
              output_schema=None, dry_run=False):
    jsonl_path = os.path.join(out_dir, "%s.jsonl" % tag)
    stderr_path = os.path.join(out_dir, "%s.stderr.log" % tag)
    last_path = os.path.join(out_dir, "%s.final.md" % tag)
    write(os.path.join(out_dir, "%s.prompt.md" % tag), prompt)

    cmd = build_codex_cmd(cfg, prompt, resume_thread=resume_thread,
                          output_schema=output_schema, output_last=last_path,
                          cwd=cfg["project_root"])
    res = CodexResult()
    res.jsonl_path, res.stderr_path = jsonl_path, stderr_path

    printable = list(cmd[:-1]) + ["<prompt %d 字>" % len(prompt)]
    dim("  $ " + " ".join(shlex.quote(x) for x in printable))
    if dry_run:
        res.returncode = 0
        res.thread_id = "dry-run-thread"
        res.final_message = "(dry-run 未真正调用 codex)"
        return res

    if not shutil.which(cfg["codex"]["bin"]):
        die("找不到 codex 可执行文件：%s（先 npm i -g @openai/codex 或改 config 里的 codex.bin）"
            % cfg["codex"]["bin"])

    with open(jsonl_path, "w", encoding="utf-8") as jf, \
         open(stderr_path, "w", encoding="utf-8") as ef:
        proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=ef,
                                text=True, bufsize=1, cwd=cfg["project_root"],
                                env=codex_env(cfg))
        try:
            for line in proc.stdout:
                jf.write(line)
                line = line.strip()
                if not line:
                    continue
                try:
                    ev = json.loads(line)
                except ValueError:
                    continue
                res.events.append(ev)
                et = ev.get("type")
                if et == "thread.started":
                    res.thread_id = ev.get("thread_id")
                    dim("    thread %s" % res.thread_id)
                elif et == "item.completed":
                    item = ev.get("item") or {}
                    if item.get("type") == "agent_message":
                        res.final_message = item.get("text", "")
                    elif item.get("type") == "command_execution":
                        cmdline = (item.get("command") or "")[:90]
                        if cmdline:
                            dim("    ⎿ %s" % cmdline)
                    elif item.get("type") == "file_change":
                        for ch in (item.get("changes") or []):
                            dim("    ± %s" % ch.get("path", ""))
                elif et == "turn.completed":
                    res.usage = ev.get("usage") or {}
                elif et == "turn.failed" or et == "error":
                    fail("    codex 报错：%s" % json.dumps(ev, ensure_ascii=False)[:300])
            proc.wait(timeout=cfg["codex"]["timeout_sec"])
        except subprocess.TimeoutExpired:
            proc.kill()
            fail("codex 超时（%ss），已终止" % cfg["codex"]["timeout_sec"])
        res.returncode = proc.returncode if proc.returncode is not None else -1

    if not res.final_message:
        res.final_message = read_if(last_path)
    return res

# ---------------------------------------------------------------- 静态闸门

class Violation(object):
    def __init__(self, rule_id, severity, path, line_no, line, message):
        self.rule_id = rule_id; self.severity = severity
        self.path = path; self.line_no = line_no
        self.line = line.rstrip(); self.message = message
    def as_dict(self):
        return dict(rule=self.rule_id, severity=self.severity, path=self.path,
                    line=self.line_no, code=self.line.strip(), message=self.message)

def _match_globs(cfg, files):
    pats = cfg.get("gate_include") or ["**/*.cs"]
    keep = []
    for f in files:
        for p in pats:
            # 简化匹配：把 ** 当作任意层级
            rx = re.escape(p).replace(r"\*\*/", "(?:.*/)?").replace(r"\*\*", ".*").replace(r"\*", "[^/]*")
            if re.fullmatch(rx, f):
                keep.append(f); break
    return keep

def run_gate(cfg, files, check_contract=True):
    """对给定文件跑正则铁律 + 契约文件保护。返回 (violations, contract_touched)"""
    root = cfg["project_root"]
    contract_set = set(cfg["contract_files"])
    cs_files = [f for f in _match_globs(cfg, [x for x in files if x.endswith(".cs")])
                if f not in contract_set]
    rules = []
    for r in cfg["gate_rules"]:
        try:
            rules.append((r, re.compile(r["pattern"], re.M)))
        except re.error as e:
            warn("闸门规则 %s 正则非法，已跳过：%s" % (r.get("id"), e))

    def rule_skips(rule, rel):
        for pat in (rule.get("exclude") or []):
            rx = (re.escape(pat).replace(r"\*\*/", "(?:.*/)?")
                  .replace(r"\*\*", ".*").replace(r"\*", "[^/]*"))
            if re.fullmatch(rx, rel):
                return True
        return False
    violations = []
    for rel in cs_files:
        p = os.path.join(root, rel)
        if not os.path.isfile(p):
            continue
        text = read_if(p)
        lines = text.splitlines()
        for r, rx in rules:
            if rule_skips(r, rel):
                continue
            for m in rx.finditer(text):
                ln = text.count("\n", 0, m.start()) + 1
                src = lines[ln - 1] if ln - 1 < len(lines) else ""
                if src.strip().startswith("//"):
                    continue
                violations.append(Violation(r["id"], r["severity"], rel, ln, src, r["message"]))
    touched = sorted(set(files) & set(cfg["contract_files"])) if check_contract else []
    return violations, touched

def gate_summary(violations, touched):
    errs = [v for v in violations if v.severity == "error"]
    warns = [v for v in violations if v.severity != "error"]
    return len(errs), len(warns), len(touched)

def print_gate(violations, touched):
    e, w, c = gate_summary(violations, touched)
    if c:
        fail("契约文件被改动（%d 个）—— 硬性违规" % c)
        for f in touched:
            print("    %s" % f)
    if not violations and not touched:
        ok("静态闸门通过：0 error 0 warn")
        return
    for v in violations:
        f = fail if v.severity == "error" else warn
        f("[%s] %s:%d  %s" % (v.rule_id, v.path, v.line_no, v.message))
        dim("      %s" % v.line.strip()[:120])
    print("  合计 %d error / %d warn" % (e, w))

# ---------------------------------------------------------------- Unity 编译

CS_ERR_RE = re.compile(r"^(?P<file>[^\s(].*?)\((?P<line>\d+),(?P<col>\d+)\):\s*error\s+(?P<code>CS\d+):\s*(?P<msg>.*)$", re.M)

def detect_unity(cfg):
    u = cfg["unity"]
    if u.get("editor_path"):
        p = expand(u["editor_path"])
        return p if os.path.exists(p) else None
    want = project_unity_version(cfg)
    found = []
    for rootdir in u.get("hub_roots", []):
        for ver_dir in sorted(glob.glob(os.path.join(expand(rootdir), "*"))):
            exe = os.path.join(ver_dir, "Unity.app", "Contents", "MacOS", "Unity")
            if not os.path.exists(exe):
                exe = os.path.join(ver_dir, "Editor", "Unity")   # Linux/Windows 布局
            if os.path.exists(exe):
                found.append((os.path.basename(ver_dir), exe))
    if not found:
        return None
    if want:
        for ver, exe in found:
            if ver == want:
                return exe
        warn("工程要求 Unity %s，但 Hub 里只有 %s —— 用了 %s，编译结果可能不代表真实情况"
             % (want, ", ".join(v for v, _ in found), found[-1][0]))
    return found[-1][1]

def project_unity_version(cfg):
    p = os.path.join(cfg["project_root"], "ProjectSettings", "ProjectVersion.txt")
    txt = read_if(p)
    m = re.search(r"m_EditorVersion:\s*(\S+)", txt)
    return m.group(1) if m else None

def unity_locked(cfg):
    """判断编辑器是不是真的开着这个工程。

    只看 Temp/UnityLockfile 存不存在是不够的：batchmode 编译一旦因为编译错误
    非正常退出，就会把这个文件留在原地。下一次跑编译就会被这具「尸体」挡住，
    而且失败得很安静 —— 恰恰在你最需要编译验证的时候（上一轮刚失败）失效。

    所以再用 lsof 确认有没有活着的进程真的持有它。lsof 不可用时退回到
    「文件存在即上锁」的保守判断。
    """
    lock = os.path.join(cfg["project_root"], "Temp", "UnityLockfile")
    if not os.path.exists(lock):
        return False
    if not shutil.which("lsof"):
        return True
    try:
        p = subprocess.run(["lsof", "--", lock], capture_output=True, text=True, timeout=20)
    except (subprocess.TimeoutExpired, OSError):
        return True
    held = bool(p.stdout.strip())
    if not held:
        warn("Temp/UnityLockfile 是上次非正常退出的残留（无进程持有），已忽略")
    return held

def parse_unity_errors(log_text, scope_prefix):
    """返回 (project_errors, package_errors)，按 file:line 去重。"""
    seen, proj, pkg = set(), [], []
    for m in CS_ERR_RE.finditer(log_text):
        d = m.groupdict()
        key = (d["file"], d["line"], d["code"])
        if key in seen:
            continue
        seen.add(key)
        entry = {"file": d["file"].strip(), "line": int(d["line"]),
                 "code": d["code"], "message": d["msg"].strip()}
        f = entry["file"].replace("\\", "/")
        if scope_prefix and scope_prefix in f:
            proj.append(entry)
        elif "/PackageCache/" in f or f.startswith("Packages/") or "/Library/PackageCache/" in f:
            pkg.append(entry)
        else:
            proj.append(entry)
    return proj, pkg

def unity_compile(cfg, out_dir, tag="unity", dry_run=False):
    """跑 Unity batchmode 编译。返回 dict。"""
    res = {"ran": False, "exit": None, "dll": False, "project_errors": [],
           "package_errors": [], "log": None, "skipped": None}
    u = cfg["unity"]
    if not u.get("enabled"):
        res["skipped"] = "配置里 unity.enabled = false"
        return res
    if dry_run:
        res["skipped"] = "dry-run"
        return res
    if unity_locked(cfg):
        res["skipped"] = "Unity 编辑器正开着这个工程（Temp/UnityLockfile 存在），batchmode 无法启动 —— 请先关闭编辑器"
        return res
    exe = detect_unity(cfg)
    if not exe:
        res["skipped"] = "没找到 Unity 编辑器可执行文件，配 unity.editor_path"
        return res

    log_path = os.path.join(out_dir, "%s.log" % tag)
    res["log"] = log_path
    cmd = [exe, "-batchmode", "-quit", "-nographics",
           "-projectPath", cfg["project_root"],
           "-logFile", log_path] + list(u.get("extra_args") or [])
    dim("  $ " + " ".join(shlex.quote(x) for x in cmd))
    try:
        p = subprocess.run(cmd, capture_output=True, text=True, timeout=u["timeout_sec"])
        res["exit"] = p.returncode
    except subprocess.TimeoutExpired:
        res["skipped"] = "Unity 编译超时（%ss）" % u["timeout_sec"]
        return res
    res["ran"] = True
    log = read_if(log_path)
    proj, pkg = parse_unity_errors(log, u.get("project_scope_prefix") or "")
    res["project_errors"], res["package_errors"] = proj, pkg
    res["dll"] = os.path.exists(os.path.join(
        cfg["project_root"], "Library", "ScriptAssemblies", "Assembly-CSharp.dll"))
    return res

def print_unity(res):
    if res.get("skipped"):
        warn("跳过编译：%s" % res["skipped"])
        return
    p, k = len(res["project_errors"]), len(res["package_errors"])
    if res["dll"] and p == 0:
        ok("编译通过，Assembly-CSharp.dll 已生成" + ("（包层还有 %d 个错误，与本任务无关）" % k if k else ""))
    else:
        fail("编译失败：_Project %d 个错误 / 包层 %d 个错误，dll=%s" % (p, k, res["dll"]))
    for e in res["project_errors"][:25]:
        print("    %s(%d) %s: %s" % (e["file"], e["line"], e["code"], e["message"][:120]))
    if p > 25:
        dim("    ...还有 %d 条，见 %s" % (p - 25, res["log"]))
    if k and p == 0:
        dim("    包层错误示例：%s" % (res["package_errors"][0]["message"][:120]))

# ---------------------------------------------------------------- 结构化自审

def build_audit_schema(cfg):
    """--output-schema 要求：additionalProperties:false，且 required 列全 key。"""
    item_props = {
        "id":       {"type": "string"},
        "verdict":  {"type": "string", "enum": ["pass", "fail", "n/a"]},
        "evidence": {"type": "string", "description": "文件:行号 或 简短引用，说明凭什么下这个判断"},
    }
    schema = {
        "type": "object",
        "additionalProperties": False,
        "required": ["overall", "items", "known_issues", "deviations"],
        "properties": {
            "overall": {"type": "string", "enum": ["pass", "fail"]},
            "items": {
                "type": "array",
                "items": {"type": "object", "additionalProperties": False,
                          "required": ["id", "verdict", "evidence"],
                          "properties": item_props},
            },
            "known_issues": {"type": "array", "items": {"type": "string"},
                             "description": "自己知道但没修的问题"},
            "deviations": {"type": "array", "items": {"type": "string"},
                           "description": "自作主张偏离任务描述的设计决策"},
        },
    }
    return schema

AUDIT_PROMPT = u"""你刚刚完成了任务 {tid}（{title}）的代码实现。

现在做**自审**。逐条核对下面 {n} 项清单，对每一项给出 pass / fail / n-a 判定，
并给出证据（文件:行号）。不要修改任何代码，只输出判定。

清单：
{checklist}

额外要求：
- 判定必须基于你实际读到的代码，不要凭记忆。需要就重新读文件。
- 任何一项 fail，overall 必须是 fail。
- known_issues 里列出你知道但没修的问题。
- deviations 里列出你自作主张偏离任务描述的设计决策（哪怕你认为更好）。

只输出符合 schema 的 JSON，不要任何额外文字。
"""

def run_audit(cfg, task, out_dir, thread_id, dry_run=False):
    checklist = "\n".join("- %s: %s" % (c["id"], c["text"]) for c in cfg["audit_checklist"])
    schema_path = write(os.path.join(out_dir, "audit.schema.json"),
                        json.dumps(build_audit_schema(cfg), ensure_ascii=False, indent=2))
    prompt = AUDIT_PROMPT.format(tid=task.id, title=task.title,
                                 n=len(cfg["audit_checklist"]), checklist=checklist)
    r = run_codex(cfg, prompt, out_dir, "audit", resume_thread=thread_id,
                  output_schema=schema_path, dry_run=dry_run)
    data = None
    txt = (r.final_message or "").strip()
    m = re.search(r"\{.*\}", txt, re.S)
    if m:
        try:
            data = json.loads(m.group(0))
        except ValueError:
            data = None
    if data:
        write(os.path.join(out_dir, "audit.json"),
              json.dumps(data, ensure_ascii=False, indent=2))
    return data, r

def print_audit(cfg, data):
    if not data:
        warn("自审没拿到结构化结果（看 audit.final.md）")
        return
    by_id = {c["id"]: c["text"] for c in cfg["audit_checklist"]}
    fails = [i for i in data.get("items", []) if i.get("verdict") == "fail"]
    passes = [i for i in data.get("items", []) if i.get("verdict") == "pass"]
    if data.get("overall") == "pass" and not fails:
        ok("Codex 自审：%d 项全过" % len(passes))
    else:
        fail("Codex 自审：%d 项不通过" % len(fails))
    for i in fails:
        print("    [%s] %s" % (i.get("id"), by_id.get(i.get("id"), "")))
        dim("      %s" % (i.get("evidence", "")[:160]))
    for k, label in (("known_issues", "已知未修"), ("deviations", "自作主张的决策")):
        vals = data.get(k) or []
        if vals:
            warn("%s（%d）：" % (label, len(vals)))
            for v in vals:
                print("    · %s" % v)

# ---------------------------------------------------------------- 提示词组装

PREAMBLE = u"""你在 Unity 6 工程 RESIDUUM（第一人称恐怖取证游戏）里干活。

架构铁律（违反直接打回，不要讨价还价）：
1. 跨模块通信一律走 `Residuum.Core.GameEvents` 静态事件总线；模块之间禁止直接 using。
   你的文件里除了 UnityEngine / System 之外，只允许 `using Residuum.Core;`。
2. 下面 7 个契约文件**一个字都不许改**。需要新事件就在最终回复里写清楚要加什么签名，
   由人类统一加，不要自己动手：
{contracts}
3. Unity 6 API：禁用 FindObjectOfType 和 FindFirstObjectByType（两者都已弃用，
   统一用 FindAnyObjectByType；要按类型取多个用 FindObjectsByType，且不要再传
   FindObjectsSortMode —— 带该参数的重载在 6000.5 已弃用）、禁用 Rigidbody.velocity
   （用 linearVelocity）、禁用旧版 Input（统一 Input System）、禁用 CinemachineVirtualCamera
   （用 CinemachineCamera）。
4. 所有可调数值必须 `[SerializeField] private` + 中文 `[Tooltip]`，不要硬编码魔法数。
5. OnEnable/OnDisable 订阅退订成对，OnDestroy 清干净，静态事件不许跨局残留。
6. 不做跳跃。不做联机。

工作目录就是工程根目录。只改 `Assets/_Project/Scripts/` 下属于本任务的文件。

---

"""

def compose_prompt(cfg, task):
    contracts = "\n".join("   - %s" % f for f in cfg["contract_files"])
    return PREAMBLE.format(contracts=contracts) + task.body

REPAIR_PROMPT = u"""你上一轮的产出没有通过本地验证。下面是**真实的机器输出**，不是我的意见。
逐条修掉，然后简短说明改了什么。不要重构无关代码，不要改契约文件。

{sections}
修完之后请自己再确认一遍：所有列出的问题都已消除。
"""

def compose_repair(gate_violations, contract_touched, unity_res):
    secs = []
    if contract_touched:
        secs.append("## 严重：你改了契约文件\n" +
                    "\n".join("- %s" % f for f in contract_touched) +
                    "\n把这些文件还原（git checkout -- <file>），需要的新事件写在回复里让人类加。\n")
    errs = [v for v in gate_violations if v.severity == "error"]
    if errs:
        lines = ["## 静态闸门 error（%d 条）" % len(errs)]
        for v in errs[:40]:
            lines.append("- %s:%d  [%s] %s\n    %s" % (v.path, v.line_no, v.rule_id,
                                                        v.message, v.line.strip()[:140]))
        secs.append("\n".join(lines) + "\n")
    pe = (unity_res or {}).get("project_errors") or []
    if pe:
        lines = ["## Unity 编译错误（_Project 范围，%d 条）" % len(pe)]
        for e in pe[:40]:
            lines.append("- %s(%d) %s: %s" % (e["file"], e["line"], e["code"], e["message"]))
        secs.append("\n".join(lines) + "\n")
    return REPAIR_PROMPT.format(sections="\n".join(secs)) if secs else None

# ---------------------------------------------------------------- 报告

def build_report(cfg, task, ctx):
    g = ctx["gate"]; u = ctx["unity"]; a = ctx["audit"]
    e_cnt, w_cnt, c_cnt = gate_summary(g["violations"], g["contract"])
    pe = len((u or {}).get("project_errors") or [])
    verdict_bits = []
    verdict_bits.append("契约完整" if c_cnt == 0 else "**契约被改 %d 个**" % c_cnt)
    verdict_bits.append("闸门 0 error" if e_cnt == 0 else "**闸门 %d error**" % e_cnt)
    if (u or {}).get("skipped"):
        verdict_bits.append("编译未验证")
    else:
        verdict_bits.append("编译通过" if (u.get("dll") and pe == 0) else "**编译失败 %d 错**" % pe)
    if a:
        verdict_bits.append("自审 %s" % a.get("overall", "?"))

    L = []
    L.append("# %s %s —— 审查包" % (task.id, task.title))
    L.append("")
    L.append("| 项 | 值 |")
    L.append("|---|---|")
    L.append("| 分支 | `%s` |" % ctx["branch"])
    L.append("| 基线 | `%s` |" % ctx["base"])
    L.append("| 运行目录 | `%s` |" % os.path.relpath(ctx["out_dir"], cfg["project_root"]))
    L.append("| Codex thread | `%s` |" % (ctx.get("thread_id") or "-"))
    L.append("| 修复轮次 | %d |" % ctx.get("repair_rounds", 0))
    L.append("| 结论 | %s |" % " · ".join(verdict_bits))
    L.append("")
    L.append("## 1. 改动文件（%d）" % len(ctx["files"]))
    L.append("")
    L.append("```")
    L.append(ctx["diffstat"] or "(无)")
    L.append("```")
    L.append("")
    L.append("## 2. 静态闸门")
    L.append("")
    if c_cnt:
        L.append("**契约文件被改动：**")
        for f in g["contract"]:
            L.append("- `%s`" % f)
        L.append("")
    if not g["violations"]:
        L.append("0 error / 0 warn。")
    else:
        L.append("| 规则 | 级别 | 位置 | 说明 |")
        L.append("|---|---|---|---|")
        for v in g["violations"]:
            L.append("| %s | %s | `%s:%d` | %s |" %
                     (v.rule_id, v.severity, v.path, v.line_no, v.message))
    L.append("")
    L.append("## 3. Unity 编译")
    L.append("")
    if (u or {}).get("skipped"):
        L.append("跳过：%s" % u["skipped"])
    else:
        L.append("- Assembly-CSharp.dll: %s" % ("已生成" if u["dll"] else "**未生成**"))
        L.append("- _Project 错误：%d" % pe)
        L.append("- 包层错误：%d（与本任务无关，属于 Unity 版本/包兼容问题）" % len(u["package_errors"]))
        if pe:
            L.append("")
            L.append("```")
            for e in u["project_errors"][:40]:
                L.append("%s(%d) %s: %s" % (e["file"], e["line"], e["code"], e["message"]))
            L.append("```")
    L.append("")
    L.append("## 4. Codex 自审（结构化）")
    L.append("")
    if not a:
        L.append("未取得结构化自审结果。")
    else:
        by_id = {c["id"]: c["text"] for c in cfg["audit_checklist"]}
        L.append("总判定：**%s**" % a.get("overall"))
        L.append("")
        L.append("| 项 | 内容 | 判定 | 证据 |")
        L.append("|---|---|---|---|")
        for i in a.get("items", []):
            L.append("| %s | %s | %s | %s |" % (
                i.get("id"), by_id.get(i.get("id"), ""), i.get("verdict"),
                (i.get("evidence") or "").replace("|", "\\|")[:120]))
        for k, label in (("known_issues", "已知未修"), ("deviations", "自作主张的设计决策")):
            vals = a.get(k) or []
            if vals:
                L.append("")
                L.append("**%s：**" % label)
                for v in vals:
                    L.append("- %s" % v)
    L.append("")
    L.append("## 5. Codex 最终回复")
    L.append("")
    L.append(ctx.get("final_message") or "(空)")
    L.append("")
    L.append("## 6. 给 Claude 终审的入口")
    L.append("")
    L.append("```bash")
    L.append("git -C %s diff %s" % (cfg["project_root"], ctx["base"]))
    L.append("```")
    L.append("")
    L.append("交叉验证要点：**自审说 pass、闸门说 error** 的项，一律以闸门为准；")
    L.append("自审全 pass 但 deviations 非空，重点看那几条决策是否可接受。")
    return "\n".join(L)

# ---------------------------------------------------------------- 主流程

def preflight(cfg, task, args):
    step("0 · 预检")
    if not os.path.isdir(cfg["project_root"]):
        die("工程目录不存在：%s" % cfg["project_root"])
    if not is_git_repo(cfg):
        die("%s 不是 git 仓库" % cfg["project_root"])
    if not os.path.isfile(os.path.join(cfg["project_root"], "AGENTS.md")):
        warn("工程根目录没有 AGENTS.md —— Codex 读不到常驻指令")
    gi = os.path.join(cfg["project_root"], ".gitignore")
    if ".codexctl/" not in read_if(gi):
        with open(gi, "a", encoding="utf-8") as f:
            f.write("\n# codexctl 运行产物\n.codexctl/\n")
        git(cfg, "add", "--", ".gitignore")
        git(cfg, "commit", "-m", "chore: gitignore .codexctl/ (codexctl)")
        info("已把 .codexctl/ 加进 .gitignore 并单独提交")
    if working_tree_dirty(cfg) and not args.allow_dirty:
        die("工作区不干净，先 commit 或 stash（要跳过用 --allow-dirty）：\n%s"
            % dirty_list(cfg))
    br = branch_for(cfg, task)
    if cfg["git"]["auto_branch"] and not args.no_branch:
        if git(cfg, "rev-parse", "--verify", br).returncode == 0:
            info("切到已有分支 %s" % br)
            git(cfg, "checkout", br, check=True)
        else:
            info("新建分支 %s" % br)
            git(cfg, "checkout", "-b", br, check=True)
    else:
        br = current_branch(cfg)
        info("留在当前分支 %s" % br)
    base = git_out(cfg, "rev-parse", "HEAD")
    ok("基线 %s @ %s" % (base[:10], br))
    return br, base

def verify(cfg, task, out_dir, base, args, tag="", slot="0"):
    """跑一遍 闸门 + 编译，返回 (gate_dict, unity_dict, files)"""
    files = changed_files(cfg, base)
    step("· 静态闸门%s" % tag)
    v, c = run_gate(cfg, files)
    print_gate(v, c)
    step("· Unity 编译%s" % tag)
    if not task.unity:
        u = {"skipped": "任务包声明 unity: false", "project_errors": [],
             "package_errors": [], "dll": False, "ran": False, "log": None}
        warn(u["skipped"])
    else:
        u = unity_compile(cfg, out_dir, tag="unity-%s" % slot, dry_run=args.dry_run)
        print_unity(u)
    return {"violations": v, "contract": c}, u, files

def verification_failed(gate, unity):
    if gate["contract"]:
        return True
    if any(x.severity == "error" for x in gate["violations"]):
        return True
    if unity and not unity.get("skipped"):
        if unity.get("project_errors") or not unity.get("dll"):
            return True
    return False

def cmd_run(cfg, args):
    task = get_task(cfg, args.task)
    out_dir = run_dir(cfg, task.id)
    print("%s%s %s%s  →  %s" % (C.BOLD, task.id, task.title, C.X,
                                os.path.relpath(out_dir, cfg["project_root"])))
    branch, base = preflight(cfg, task, args)

    step("1 · Codex 实现")
    prompt = compose_prompt(cfg, task)
    r = run_codex(cfg, prompt, out_dir, "impl", dry_run=args.dry_run)
    if r.returncode != 0:
        warn("codex 退出码 %d（继续做验证，看看产出到什么程度）" % r.returncode)
    if r.usage:
        dim("  token: in %s / cached %s / out %s" % (
            r.usage.get("input_tokens"), r.usage.get("cached_input_tokens"),
            r.usage.get("output_tokens")))
    thread_id = r.thread_id
    final_msg = r.final_message

    gate, unity, files = verify(cfg, task, out_dir, base, args)

    rounds = 0
    while verification_failed(gate, unity) and rounds < cfg["repair"]["max_attempts"] and not args.no_repair:
        rounds += 1
        step("2.%d · 打回重修（第 %d 轮 / 上限 %d）" % (rounds, rounds, cfg["repair"]["max_attempts"]))
        rp = compose_repair(gate["violations"], gate["contract"], unity)
        if not rp:
            break
        rr = run_codex(cfg, rp, out_dir, "repair%d" % rounds,
                       resume_thread=thread_id, dry_run=args.dry_run)
        final_msg = rr.final_message or final_msg
        gate, unity, files = verify(cfg, task, out_dir, base, args,
                                    tag="（修复 %d 轮后）" % rounds, slot="r%d" % rounds)

    step("3 · Codex 结构化自审")
    audit = None
    if not args.no_audit:
        audit, _ = run_audit(cfg, task, out_dir, thread_id, dry_run=args.dry_run)
        print_audit(cfg, audit)
    else:
        dim("  已跳过")

    step("4 · 生成审查包")
    ctx = {"branch": branch, "base": base, "out_dir": out_dir, "files": files,
           "diffstat": diffstat(cfg, base), "gate": gate, "unity": unity,
           "audit": audit, "thread_id": thread_id, "final_message": final_msg,
           "repair_rounds": rounds}
    report = build_report(cfg, task, ctx)
    rp_path = write(os.path.join(out_dir, "report.md"), report)
    write(os.path.join(out_dir, "diff.patch"), full_diff(cfg, base))
    write(os.path.join(out_dir, "context.json"), json.dumps({
        "task": task.id, "title": task.title, "branch": branch, "base": base,
        "thread_id": thread_id, "files": files, "repair_rounds": rounds,
        "gate": [v.as_dict() for v in gate["violations"]],
        "contract_touched": gate["contract"],
        "unity": {k: unity.get(k) for k in ("skipped", "dll", "project_errors", "package_errors")},
        "audit_overall": (audit or {}).get("overall"),
    }, ensure_ascii=False, indent=2))

    # 最新一次的软链，方便 Claude 固定路径读
    latest = os.path.join(cfg["project_root"], ".codexctl", "runs", task.id.upper(), "latest")
    try:
        if os.path.islink(latest) or os.path.exists(latest):
            os.remove(latest)
        os.symlink(os.path.basename(out_dir), latest)
    except OSError:
        pass

    ok("审查包：%s" % os.path.relpath(rp_path, cfg["project_root"]))

    if cfg["git"]["auto_commit"] and not args.dry_run and files:
        git(cfg, "add", "-A")
        git(cfg, "commit", "-m", "%s %s (codexctl)" % (task.id, task.title))
        ok("已提交到 %s" % branch)

    blocked = verification_failed(gate, unity)
    print("")
    if blocked:
        fail("状态：未通过本地验证，别叫 Claude 终审，先看 report.md")
    else:
        ok("状态：本地验证全绿，可以交 Claude 终审")
    print("  Claude Code 里直接说：审查 %s，读 .codexctl/runs/%s/latest/report.md"
          % (task.id, task.id.upper()))
    return 1 if blocked else 0

# ---------------------------------------------------------------- 其它子命令

def cmd_gate(cfg, args):
    base = args.base or cfg["git"]["base_branch"]
    files = changed_files(cfg, base) if not args.all else [
        os.path.relpath(p, cfg["project_root"])
        for p in glob.glob(os.path.join(cfg["project_root"], "Assets/_Project/**/*.cs"), recursive=True)]
    info("检查 %d 个文件（基线 %s）" % (len(files), "全量" if args.all else base))
    v, c = run_gate(cfg, files, check_contract=not args.all)
    print_gate(v, c)
    return 1 if (c or any(x.severity == "error" for x in v)) else 0

def cmd_compile(cfg, args):
    out = run_dir(cfg, "_compile")
    res = unity_compile(cfg, out, dry_run=False)
    print_unity(res)
    if res.get("log"):
        dim("  完整日志：%s" % res["log"])
    return 0 if (res.get("dll") and not res.get("project_errors")) else 1

def cmd_audit(cfg, args):
    task = get_task(cfg, args.task)
    d = latest_run_dir(cfg, task.id)
    if not d:
        die("%s 还没有跑过，先 codexctl run %s" % (task.id, task.id))
    ctx = json.loads(read_if(os.path.join(d, "context.json")) or "{}")
    tid = args.thread or ctx.get("thread_id")
    if not tid:
        die("找不到 codex thread id，无法 resume")
    data, _ = run_audit(cfg, task, d, tid, dry_run=args.dry_run)
    print_audit(cfg, data)
    return 0 if (data or {}).get("overall") == "pass" else 1

def cmd_report(cfg, args):
    task = get_task(cfg, args.task)
    d = latest_run_dir(cfg, task.id)
    if not d:
        die("%s 没有运行记录" % task.id)
    p = os.path.join(d, "report.md")
    if not os.path.isfile(p):
        die("没有 report.md：%s" % d)
    print(read_if(p))
    return 0

def cmd_list(cfg, args):
    tasks = load_tasks(cfg)
    if not tasks:
        warn("tasks/ 目录是空的：%s" % tasks_dir(cfg))
        return 1
    for tid in sorted(tasks):
        t = tasks[tid]
        d = latest_run_dir(cfg, tid)
        state = "-"
        if d:
            ctx = json.loads(read_if(os.path.join(d, "context.json")) or "{}")
            errs = len(ctx.get("gate") or []) + len(ctx.get("contract_touched") or [])
            pe = len((ctx.get("unity") or {}).get("project_errors") or [])
            state = "%s 闸门%d 编译错%d 自审%s" % (
                os.path.basename(d), errs, pe, ctx.get("audit_overall") or "?")
        print("%-6s %-24s deps=%-8s unity=%-5s %s" % (
            t.id, t.title, ",".join(t.depends) or "-", t.unity, state))
    return 0

def cmd_doctor(cfg, args):
    step("codexctl v%s 自检" % VERSION)
    print("配置文件：%s" % (cfg.get("_config_path") or "(用内置默认值)"))
    print("工程目录：%s" % cfg["project_root"])
    bad = 0
    def chk(cond, good, badmsg):
        nonlocal bad
        (ok if cond else fail)(good if cond else badmsg)
        if not cond: bad += 1

    chk(os.path.isdir(cfg["project_root"]), "工程目录存在", "工程目录不存在")
    chk(is_git_repo(cfg), "是 git 仓库（当前分支 %s）" % current_branch(cfg), "不是 git 仓库")
    chk(os.path.isfile(os.path.join(cfg["project_root"], "AGENTS.md")),
        "AGENTS.md 就位", "缺 AGENTS.md，Codex 读不到常驻指令")
    cb = shutil.which(cfg["codex"]["bin"])
    chk(bool(cb), "codex: %s" % cb, "找不到 codex（npm i -g @openai/codex）")
    if cb:
        try:
            ver = subprocess.run([cb, "--version"], capture_output=True, text=True, timeout=20)
            dim("   %s" % (ver.stdout or ver.stderr).strip().splitlines()[0])
        except Exception:
            pass
    auth = expand(os.path.join(os.environ.get("CODEX_HOME", "~/.codex"), "auth.json"))
    chk(os.path.exists(auth) or os.environ.get("CODEX_API_KEY"),
        "codex 已登录（%s）" % auth, "codex 未登录，跑一次 codex login")
    if os.environ.get("OPENAI_API_KEY"):
        warn("环境里有 OPENAI_API_KEY —— codexctl 会在调用时屏蔽它，走订阅额度而非 API 计费")
    pv = project_unity_version(cfg)
    print("工程 Unity 版本：%s" % (pv or "读不到 ProjectVersion.txt"))
    ue = detect_unity(cfg)
    chk(bool(ue), "Unity 编辑器：%s" % ue, "没找到匹配的 Unity 编辑器，配 unity.editor_path")
    if unity_locked(cfg):
        warn("Unity 编辑器正开着这个工程 —— batchmode 编译会被锁，跑编译前先关掉")
    dll = os.path.join(cfg["project_root"], "Library", "ScriptAssemblies", "Assembly-CSharp.dll")
    (ok if os.path.exists(dll) else warn)(
        "Assembly-CSharp.dll 存在" if os.path.exists(dll)
        else "Assembly-CSharp.dll 不存在 —— 工程当前编译不过（这正是要修的阻塞）")
    t = load_tasks(cfg)
    print("任务包：%d 个（%s）" % (len(t), tasks_dir(cfg)))
    print("契约文件：%d 个受保护" % len(cfg["contract_files"]))
    print("闸门规则：%d 条 / 自审清单：%d 项" % (len(cfg["gate_rules"]), len(cfg["audit_checklist"])))
    return 1 if bad else 0

def cmd_probe(cfg, args):
    """用一个最小任务真跑一遍 codex，验证 exec / resume / --output-schema
    这三种调用在你本机这个 codex 版本上的参数顺序是对的。
    只读沙箱，不会改任何文件。"""
    step("探针：验证 codex 调用约定（只读，不改文件）")
    out = run_dir(cfg, "_PROBE")
    saved = cfg["codex"]["sandbox"]
    cfg["codex"]["sandbox"] = "read-only"
    bad = 0

    info("1/3 exec --json")
    r1 = run_codex(cfg, "只回复两个字：收到。不要做任何别的事。", out, "probe1")
    if r1.returncode == 0 and r1.thread_id:
        ok("exec 正常，thread=%s" % r1.thread_id)
    else:
        fail("exec 失败（退出码 %s）见 %s" % (r1.returncode, r1.stderr_path)); bad += 1

    if r1.thread_id:
        info("2/3 exec resume <thread>")
        r2 = run_codex(cfg, "我刚才让你回复什么？原样再说一遍。", out, "probe2",
                       resume_thread=r1.thread_id)
        if r2.returncode == 0:
            ok("resume 正常：%s" % (r2.final_message or "")[:60])
        else:
            fail("resume 失败 —— 打回重修和自审都会用它。见 %s" % r2.stderr_path); bad += 1

        info("3/3 --output-schema 结构化输出")
        sch = write(os.path.join(out, "probe.schema.json"), json.dumps({
            "type": "object", "additionalProperties": False,
            "required": ["answer"], "properties": {"answer": {"type": "string"}}},
            ensure_ascii=False))
        r3 = run_codex(cfg, "把上一条回复放进 answer 字段，只输出 JSON。", out, "probe3",
                       resume_thread=r1.thread_id, output_schema=sch)
        try:
            json.loads(re.search(r"\{.*\}", r3.final_message or "", re.S).group(0))
            ok("--output-schema 正常")
        except Exception:
            fail("--output-schema 没拿到合法 JSON —— 自审会退化成纯文本。见 %s" % r3.stderr_path)
            bad += 1

    cfg["codex"]["sandbox"] = saved
    print("")
    (ok if not bad else fail)("探针结束，%d 项异常" % bad)
    if bad:
        dim("  参数顺序可能随 codex 版本变了，改 build_codex_cmd() 里的拼装顺序即可。")
    return 1 if bad else 0


def cmd_initconfig(cfg, args):
    p = args.out or os.path.join(os.getcwd(), "codexctl.config.json")
    if os.path.exists(p) and not args.force:
        die("已存在 %s（要覆盖加 --force）" % p)
    d = json.loads(json.dumps(DEFAULT_CONFIG))
    write(p, json.dumps(d, ensure_ascii=False, indent=2))
    ok("已写出 %s" % p)
    return 0

# ---------------------------------------------------------------- CLI

def main(argv=None):
    ap = argparse.ArgumentParser(
        prog="codexctl",
        description="RESIDUUM · Codex CLI 中转控制器 v%s" % VERSION,
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=textwrap.dedent(u"""\
            典型用法：
              codexctl doctor                 环境自检（第一次跑这个）
              codexctl list                   看任务包和各自状态
              codexctl run T02                跑完整链路：实现→闸门→编译→打回重修→自审→报告
              codexctl run T02 --dry-run      只打印会执行的命令，不真跑
              codexctl gate --all             只跑静态铁律检查
              codexctl compile                只跑 Unity batchmode 编译
              codexctl report T02             打印最近一次审查包
        """))
    ap.add_argument("--config", help="指定配置文件")
    ap.add_argument("--project", help="覆盖工程根目录")
    sub = ap.add_subparsers(dest="cmd")

    p = sub.add_parser("run", help="跑完整任务链路")
    p.add_argument("task")
    p.add_argument("--dry-run", action="store_true", help="只打印命令不执行")
    p.add_argument("--allow-dirty", action="store_true", help="允许工作区不干净")
    p.add_argument("--no-branch", action="store_true", help="不自动建分支")
    p.add_argument("--no-repair", action="store_true", help="失败不打回重修")
    p.add_argument("--no-audit", action="store_true", help="跳过结构化自审")
    p.set_defaults(fn=cmd_run)

    p = sub.add_parser("gate", help="只跑静态铁律检查")
    p.add_argument("--base", help="对比基线 ref")
    p.add_argument("--all", action="store_true", help="全量扫 Assets/_Project")
    p.set_defaults(fn=cmd_gate)

    p = sub.add_parser("compile", help="只跑 Unity batchmode 编译")
    p.set_defaults(fn=cmd_compile)

    p = sub.add_parser("audit", help="对已有 thread 重跑结构化自审")
    p.add_argument("task")
    p.add_argument("--thread", help="指定 codex thread id")
    p.add_argument("--dry-run", action="store_true")
    p.set_defaults(fn=cmd_audit)

    p = sub.add_parser("report", help="打印最近一次审查包")
    p.add_argument("task")
    p.set_defaults(fn=cmd_report)

    p = sub.add_parser("list", help="列出任务包与状态")
    p.set_defaults(fn=cmd_list)

    p = sub.add_parser("doctor", help="环境自检")
    p.set_defaults(fn=cmd_doctor)

    p = sub.add_parser("probe", help="真跑一次 codex，验证 exec/resume/schema 调用约定")
    p.set_defaults(fn=cmd_probe)

    p = sub.add_parser("init-config", help="写出一份默认配置")
    p.add_argument("--out")
    p.add_argument("--force", action="store_true")
    p.set_defaults(fn=cmd_initconfig)

    args = ap.parse_args(argv)
    if not getattr(args, "fn", None):
        ap.print_help()
        return 0
    cfg = load_config(args.config)
    if args.project:
        cfg["project_root"] = expand(args.project)
    for a in ("dry_run", "allow_dirty", "no_branch", "no_repair", "no_audit"):
        if not hasattr(args, a):
            setattr(args, a, False)
    return args.fn(cfg, args)

if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        print("")
        die("已中断", 130)
