#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
R2 分层 COM 口径（单点定义，供各门卡 import，零第三方依赖）。

B2 的 ADR-v2-004 D3 把「产品程序集出现 ComImport / InterfaceType / Guid 声明」
一律判红。R2 起修订为分层口径：真实 IFileOperation 绑定必须存在，但 COM 声明
只能集中在唯一白名单文件；其余层（Core / Fs 其他文件 / App / Host / Tests）
保持零声明，且全产品禁止函数指针式 vtable、verb 查询与菜单删项。

R4 起第二套 COM 声明（IContextMenu / IContextMenu2/3 / IShellFolder）同样只落在
本白名单文件。SDK vtable 第三槽必须与头文件逐字对应，因此白名单里允许出现
该方法名的**声明**，但不允许任何调用形态（`.MethodName(`）；其余层连声明也禁止。
口径由 verb_hit 单点定义，check-boundaries / check-fs / check-menu / check-transport
/ check-selftest / check-release / open-source 一律复用，不各自另立一套。

R5 起第三套 COM 声明（IShellItemImageFactory，IUnknown 之后仅 GetImage 一槽）
并入同一白名单；GUID 与 vtable 顺序同样由 EXPECTED_IDS / EXPECTED_METHODS 单点定义，
check-transport-specs 遍历全表自动覆盖，不需要为新接口另立一套口径。
图片缩略图的 WIC 自解码改用 WPF 内置托管包装，不新增手写 COM 声明（ADR-015 D2）。
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
# 调用形态：白名单文件也不允许，SDK vtable 声明才算合规（R4 单点口径）。
VERB_CALL = re.compile(r"\.\s*GetCommandString\s*\(")
MENU_DELETION = re.compile(r"\bDeleteMenu\s*\(")

# Windows SDK 10.0.19041.0 权威值（um/ShObjIdl_core.h、um/ShObjIdl_core.idl）
EXPECTED_IDS = {
    "FileOperationClsid": "3ad05575-8857-4850-9277-11b85bdb8e09",
    "FileOperationIid": "947aab5f-0a5c-4c13-b4d6-4bf7836fc9f8",
    "ProgressSinkIid": "04b0f1a7-9490-44bc-96e1-4296a31252e2",
    "ShellItemIid": "43826d1e-e718-42ee-bc55-a1e261c37bfe",
    # R4：um/ShObjIdl_core.h 的 MIDL_INTERFACE / C 风格 vtable 权威值
    "ContextMenuIid": "000214e4-0000-0000-c000-000000000046",
    "ContextMenu2Iid": "000214f4-0000-0000-c000-000000000046",
    "ContextMenu3Iid": "bcfce0a0-ec17-11d0-8d10-00a0c90f2719",
    "ShellFolderIid": "000214e6-0000-0000-c000-000000000046",
    # R5：um/ShObjIdl_core.h 8693 行 MIDL_INTERFACE 权威值
    "ImageFactoryIid": "bcc18b79-ba16-442f-80c4-8a59c30c463b",
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
    # R4：三个菜单接口各自列出 IUnknown 之后的全部槽位，逐字对应 SDK vtable。
    "IContextMenu": [
        "QueryContextMenu", "InvokeCommand", "GetCommandString",
    ],
    "IContextMenu2": [
        "QueryContextMenu", "InvokeCommand", "GetCommandString", "HandleMenuMsg",
    ],
    "IContextMenu3": [
        "QueryContextMenu", "InvokeCommand", "GetCommandString",
        "HandleMenuMsg", "HandleMenuMsg2",
    ],
    "IShellFolder": [
        "ParseDisplayName", "EnumObjects", "BindToObject", "BindToStorage",
        "CompareIDs", "CreateViewObject", "GetAttributesOf", "GetUIObjectOf",
        "GetDisplayNameOf", "SetNameOf",
    ],
    # R5：um/ShObjIdl_core.h 8697–8700 行，IUnknown 之后只有 GetImage 一槽。
    "IShellItemImageFactory": ["GetImage"],
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


def verb_hit(rel_posix, text):
    """verb 查询单点口径。

    白名单文件：允许 SDK vtable 的方法名声明，但任何调用形态都判红；
    其余文件：出现该方法名即判红（沿用 B-04 / B5 以来的旧口径）。
    返回命中串或 None，便于各门卡直接拼进红条明细。
    """
    if is_declaration(rel_posix):
        m = VERB_CALL.search(text or "")
    else:
        m = VERB_SCAN.search(text or "")
    return m.group(0) if m else None
