#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
R2 分层 COM 口径（单点定义，供各门卡 import，零第三方依赖）。

B2 的 ADR-v2-004 D3 把「产品程序集出现 ComImport / InterfaceType / Guid 声明」
一律判红。R2 起修订为分层口径：真实 IFileOperation 绑定必须存在，但 COM 声明
只能集中在唯一白名单文件；其余层（Core / Fs 其他文件 / App / Host / Tests）
保持零声明，且全产品禁止函数指针式 vtable、verb 查询与菜单删项。

本模块只定义口径，不做扫描；扫描由 check_fs_specs / check_menu_specs /
check_icon_specs / check_realization_specs / check_transport_specs 各自执行。
"""
import re

COM_DECLARATION_FILE = "src/QuadLite.Fs/ShellComInterop.cs"

# 托管 COM 声明（白名单文件之外一律禁止）
HAND_WRITTEN_COM = re.compile(r"\bComImport\b|\bInterfaceType\b|\[\s*Guid\s*\(")

# 函数指针 / unsafe 式手写 vtable：任何地方都禁止
RAW_VTABLE = re.compile(
    r"GetDelegateForFunctionPointer|GetFunctionPointerForDelegate|"
    r"Marshal\.ReadIntPtr|Marshal\.WriteIntPtr|\bunsafe\b|stackalloc|fixed\s*\(")

VERB_SCAN = re.compile(r"\bGetCommandString\b")
MENU_DELETION = re.compile(r"\bDeleteMenu\s*\(")

# Windows SDK 10.0.19041.0 权威值（um/ShObjIdl_core.h、um/ShObjIdl_core.idl）
EXPECTED_IDS = {
    "FileOperationClsid": "3ad05575-8857-4850-9277-11b85bdb8e09",
    "FileOperationIid": "947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8",
    "ProgressSinkIid": "04b0f1a7-9490-44bc-96e1-4296a31252e2",
    "ShellItemIid": "43826d1e-e718-42ee-bc55-a1e261c37bfe",
}

# vtable 方法顺序，逐字取自同一份 SDK 头文件
EXPECTED_METHODS = {
    "IFileOperation": [
        "Advise", "Unadvise", "SetOperationFlags", "SetProgressMessage",
        "SetProgressDialog", "SetProperties", "SetOwnerWindow",
        "ApplyPropertiesToItem", "ApplyPropertiesToItems", "RenameItem",
        "RenameItems", "MoveItem", "MoveItems", "CopyItem", "CopyItems",
        "DeleteItem", "DeleteItems", "NewItem", "PerformOperations",
        "GetAnyOperationsAborted",
    ],
    "IFileOperationProgressSink": [
        "StartOperations", "FinishOperations", "PreRenameItem", "PostRenameItem",
        "PreMoveItem", "PostMoveItem", "PreCopyItem", "PostCopyItem",
        "PreDeleteItem", "PostDeleteItem", "PreNewItem", "PostNewItem",
        "UpdateProgress", "ResetTimer", "PauseTimer", "ResumeTimer",
    ],
    "IShellItem": [
        "BindToHandler", "GetParent", "GetDisplayName", "GetAttributes", "Compare",
    ],
}


def is_declaration(rel_posix):
    """rel_posix 是相对仓库根、用 / 分隔的路径。"""
    return rel_posix == COM_DECLARATION_FILE


def declaration_hit(rel_posix, text):
    """在非白名单文件里命中 COM 声明时返回命中串，否则返回 None。"""
    if is_declaration(rel_posix):
        return None
    m = HAND_WRITTEN_COM.search(text or "")
    return m.group(0) if m else None
