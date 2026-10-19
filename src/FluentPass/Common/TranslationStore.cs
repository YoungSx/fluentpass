using System;
using System.Collections.Generic;

namespace FluentPass.Common
{
    public static class TranslationStore
    {
        private static readonly Dictionary<string, string> ZhStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Common & Navigation
            ["App_Title"] = "Fluent Pass",
            ["App_TitleLocked"] = "Fluent Pass (已锁定)",
            ["Common_Save"] = "保存",
            ["Common_Cancel"] = "取消",
            ["Common_Edit"] = "编辑",
            ["Common_Delete"] = "删除",
            ["Common_New"] = "新建",
            ["Common_Restore"] = "还原",
            ["Common_Close"] = "关闭",
            ["Common_Settings"] = "设置中心",
            ["Common_Prompt"] = "提示",
            ["Common_Confirm"] = "确认",
            ["Common_Copy"] = "复制",
            ["Common_Unknown"] = "未知",
            ["Common_Untitled"] = "(无标题)",
            ["Common_UntitledEntry"] = "(无标题条目)",

            // TitleBar
            ["TitleBar_SaveTooltip"] = "保存数据库 (Ctrl+S)",
            ["TitleBar_LockTooltip"] = "锁定数据库 (Ctrl+L)",
            ["TitleBar_SearchPlaceholder"] = "实时搜索条目 (Ctrl+F)...",
            ["TitleBar_ToggleKeePassTooltip"] = "切换原版 KeePass 主窗口显示/隐藏",
            ["TitleBar_ThemeTooltip"] = "切换外观主题",
            ["TitleBar_ThemeDark"] = "深色主题 (Dark)",
            ["TitleBar_ThemeLight"] = "浅色主题 (Light)",
            ["TitleBar_ThemeSystem"] = "跟随系统 (System)",
            ["TitleBar_SettingsTooltip"] = "设置中心 (Ctrl+,)",

            // Database States
            ["DbState_LockedTitle"] = "数据库已锁定",
            ["DbState_LockedPrompt"] = "为了保障敏感凭据安全，当前密码数据库已进入加密锁定保护状态。",
            ["DbState_UnlockButton"] = "立即解锁数据库",
            ["DbState_NotOpenTitle"] = "未打开任何密码数据库",
            ["DbState_NotOpenPrompt"] = "请在 KeePass 中打开或创建 KDBX 数据库",
            ["DbState_OpenOriginalButton"] = "呼出原版窗口进行操作",

            // Groups
            ["Groups_Header"] = "分组目录",
            ["Groups_EmptyRecycleBinTooltip"] = "清空回收站",
            ["Groups_NewSubgroupTooltip"] = "在选中分组下新建子分组",
            ["Groups_DeleteGroupTooltip"] = "删除当前分组",
            ["Groups_NewGroupName"] = "新分组",

            // Entries List
            ["Entries_SummaryFormat"] = "共 {0} 个条目 (显示 {1} 项)",
            ["Entries_NewButton"] = "新建",
            ["Entries_EmptyTitle"] = "无密码条目",
            ["Entries_EmptyPrompt"] = "当前分组下暂无条目，可点击上方「+ 新建」",
            ["Entries_ContextMenu_CopyPassword"] = "复制密码",
            ["Entries_ContextMenu_CopyUsername"] = "复制用户名",
            ["Entries_ContextMenu_CopyUrl"] = "复制网址",
            ["Entries_ContextMenu_OpenInBrowser"] = "在浏览器中打开",
            ["Entries_ContextMenu_AutoType"] = "执行自动输入 (Auto-Type)",
            ["Entries_ContextMenu_EditEntry"] = "编辑条目",
            ["Entries_ContextMenu_DeleteEntry"] = "删除条目",
            ["Entries_ContextMenu_OpenOriginal"] = "调起原版 KeePass 编辑窗口",

            // Entry Detail
            ["Detail_NoSelectionTitle"] = "未选择条目",
            ["Detail_NoSelectionPrompt"] = "从左侧或中间列表选择一条凭据以查看详情",
            ["Detail_ModifiedFormat"] = "修改于 {0}",
            ["Detail_TitlePlaceholder"] = "条目标题",
            ["Detail_AutoTypeTooltip"] = "执行全局自动输入 (Auto-Type)",
            ["Detail_OpenOriginalTooltip"] = "调起原版 KeePass 高级编辑窗口",
            ["Detail_UserNameLabel"] = "用户名 (User Name)",
            ["Detail_UserNameEmpty"] = "（未设置用户名）",
            ["Detail_UserNamePlaceholder"] = "请输入用户名",
            ["Detail_CopyUserNameTooltip"] = "复制用户名",
            ["Detail_PasswordLabel"] = "密码 (Password)",
            ["Detail_PasswordAutoMaskFormat"] = "{0} 秒后自动遮蔽",
            ["Detail_TogglePasswordTooltip"] = "显示/隐藏明文 (10秒后自动遮蔽)",
            ["Detail_CopyPasswordTooltip"] = "安全复制密码 (原版剪贴板倒计时自动清空)",
            ["Detail_PasswordPlaceholder"] = "输入密码",
            ["Detail_ToggleEditPasswordTooltip"] = "显示/隐藏明文 (防窥保护)",
            ["Detail_PwGenTooltip"] = "调起原版密码生成器",
            ["Detail_UrlLabel"] = "网址 (URL)",
            ["Detail_UrlEmpty"] = "（未设置网址）",
            ["Detail_UrlPlaceholder"] = "https://...",
            ["Detail_OpenUrlTooltip"] = "在默认浏览器中打开",
            ["Detail_CopyUrlTooltip"] = "复制网址",
            ["Detail_NotesHeader"] = "备注 (Notes)",
            ["Detail_NotesEmpty"] = "（无备注内容）",
            ["Detail_NotesPlaceholder"] = "请输入备注信息...",
            ["Detail_CustomFieldsHeader"] = "自定义附加字段",
            ["Detail_CustomFieldsRevealTooltip"] = "查看明文",
            ["Detail_AttachmentsHeader"] = "二进制附件",
            ["Detail_AttachmentSaveTooltip"] = "保存附件到本地",
            ["Detail_AttachmentViewTooltip"] = "调起原版窗口查看附件",
            ["Detail_HistoryHeader"] = "历史版本快照 ({0})",
            ["Detail_HistoryTimeFormat"] = "快照时间: {0}",
            ["Detail_HistoryTitleFormat"] = "条目标题: {0}",
            ["Detail_HistoryRestoreTooltip"] = "将条目还原至此历史版本 (当前状态将自动创建新快照)",
            ["Detail_CreatedFormat"] = "创建时间: {0}",
            ["Detail_LastModifiedFormat"] = "最后修改: {0}",

            // Password Quality
            ["PwQuality_NotSet"] = "未设置",
            ["PwQuality_NoPassword"] = "无密码",
            ["PwQuality_VeryWeakFormat"] = "非常弱 ({0} 位)",
            ["PwQuality_WeakFormat"] = "弱 ({0} 位)",
            ["PwQuality_ModerateFormat"] = "中等 ({0} 位)",
            ["PwQuality_StrongFormat"] = "强 ({0} 位)",

            // Settings View
            ["Settings_BackTooltip"] = "返回密码工作区 (Esc)",
            ["Settings_Title"] = "设置中心",
            ["Settings_SectionBehavior"] = "行为与接管",
            ["Settings_AutoTakeoverTitle"] = "启动时自动接管 KeePass",
            ["Settings_AutoTakeoverDesc"] = "在 KeePass 启动后自动弹出 Fluent 现代界面，并隐藏 KeePass 原版主窗口。",
            ["Settings_CloseActionTitle"] = "关闭窗口时的行为",
            ["Settings_CloseActionDesc"] = "选择点击 Fluent 窗口右上角关闭按钮时，如何处理原版 KeePass。",
            ["Settings_CloseAction_Restore"] = "恢复原版 KeePass 窗口",
            ["Settings_CloseAction_Tray"] = "最小化到系统托盘",
            ["Settings_CloseAction_Exit"] = "退出 KeePass 进程",
            ["Settings_SectionAppearance"] = "外观与个性化",
            ["Settings_ThemeTitle"] = "界面外观主题",
            ["Settings_ThemeDesc"] = "切换深色、浅色或自动跟随 Windows 11 系统主题。",
            ["Settings_Theme_System"] = "跟随系统",
            ["Settings_Theme_Light"] = "浅色主题",
            ["Settings_Theme_Dark"] = "深色主题",
            ["Settings_LanguageTitle"] = "界面显示语言",
            ["Settings_LanguageDesc"] = "选择 Fluent Pass 的显示语言，支持动态即时切换。",
            ["Settings_Language_Auto"] = "跟随 KeePass / 系统 (Auto)",
            ["Settings_Language_Zh"] = "简体中文 (Chinese Simplified)",
            ["Settings_Language_En"] = "English (英语)",
            ["Settings_SectionSecurity"] = "安全与防护",
            ["Settings_AutoMaskTitle"] = "明文密码自动遮蔽时间",
            ["Settings_AutoMaskDesc"] = "查看密码明文后，经过设定时间自动恢复掩码状态以防窥探。",
            ["Settings_Seconds5"] = "5 秒",
            ["Settings_Seconds10Rec"] = "10 秒 (推荐)",
            ["Settings_Seconds15"] = "15 秒",
            ["Settings_Seconds30"] = "30 秒",
            ["Settings_SectionAbout"] = "关于",
            ["Settings_VersionFormat"] = "版本 {0} · 基于微软 WPF-UI 现代设计",
            ["Settings_ResetButton"] = "恢复默认设置",

            // Status Bar & Operations
            ["Status_Ready"] = "就绪",
            ["Status_ReadyUnsaved"] = "就绪 (存在未保存的修改)",
            ["Status_NotOpen"] = "未打开数据库",
            ["Status_UnnamedDb"] = "未命名数据库",
            ["Status_Locked"] = "数据库已锁定",
            ["Status_LockedClickToUnlock"] = "数据库已锁定，请点击解锁",
            ["Status_Unlocked"] = "数据库已解锁",
            ["Status_Saved"] = "数据库已保存",
            ["Status_SaveCancelled"] = "保存已取消或未完成",
            ["Status_SaveFailedFormat"] = "保存失败: {0}",
            ["Status_LockFailedFormat"] = "锁定失败: {0}",
            ["Status_UnlockFailedFormat"] = "解锁失败: {0}",
            ["Status_ToggleWindowFailedFormat"] = "切换窗口失败: {0}",
            ["Status_OriginalWindowHidden"] = "原版窗口已隐藏",
            ["Status_OriginalWindowShown"] = "原版窗口已显示",
            ["Status_EntryCreated"] = "已新建条目",
            ["Status_EntryCreateFailedFormat"] = "新建条目失败: {0}",
            ["Status_EntryDeletedFormat"] = "已删除条目: {0}",
            ["Status_EntryDeleteFailedFormat"] = "删除条目失败: {0}",
            ["Status_GroupCreated"] = "已新建分组",
            ["Status_GroupCreateFailedFormat"] = "新建分组失败: {0}",
            ["Status_GroupDeleted"] = "已删除分组",
            ["Status_GroupDeleteFailedFormat"] = "删除分组失败: {0}",
            ["Status_RecycleBinEmptied"] = "已清空回收站",
            ["Status_RecycleBinEmptyFailedFormat"] = "清空回收站失败: {0}",
            ["Status_CopiedPasswordFormat"] = "已复制密码: {0}",
            ["Status_CopiedPasswordFailedFormat"] = "复制密码失败: {0}",
            ["Status_CopiedUsernameFormat"] = "已复制用户名: {0}",
            ["Status_CopiedUsernameFailedFormat"] = "复制用户名失败: {0}",
            ["Status_CopiedUrlFormat"] = "已复制网址: {0}",
            ["Status_CopiedUrlFailedFormat"] = "复制网址失败: {0}",
            ["Status_OpenUrlFailedFormat"] = "打开链接失败: {0}",
            ["Status_AutoTypeFailedFormat"] = "执行自动输入失败: {0}",
            ["Status_OpenOriginalDialogFailedFormat"] = "调起原版编辑窗口失败: {0}",

            // Dialogs & Notifications
            ["Dialog_UnsavedChanges_Prompt"] = "当前数据库有未保存的更改。是否立即保存？",
            ["Dialog_UnsavedChanges_Title"] = "未保存的更改",
            ["Dialog_DeleteEntry_ConfirmFormat"] = "确定要删除条目 \"{0}\" 吗？",
            ["Dialog_DeleteEntry_Title"] = "确认删除",
            ["Dialog_RecycleBinAlreadyEmpty"] = "回收站当前已为空。",
            ["Dialog_EmptyRecycleBin_ConfirmFormat"] = "确定要彻底清空回收站中的所有 {0} 个项目吗？\n此操作将永久删除这些凭据且不可撤销！",
            ["Dialog_EmptyRecycleBin_Title"] = "清空回收站",
            ["Dialog_CannotDeleteRootGroup"] = "不能删除根分组！",
            ["Dialog_DeleteGroup_ConfirmFormat"] = "确定要删除分组 \"{0}\" 及其内部所有条目吗？",
            ["Dialog_DeleteGroup_Title"] = "确认删除分组",
            ["Dialog_RestoreHistory_ConfirmFormat"] = "确定要将当前条目还原为 {0} 的历史版本吗？\n当前内容将自动备份为新的历史版本。",
            ["Dialog_RestoreHistory_Title"] = "确认还原历史版本",
            ["Dialog_RestoreHistory_FailedFormat"] = "还原版本失败: {0}",
            ["Dialog_SaveEntry_FailedFormat"] = "保存修改失败: {0}",
            ["Dialog_PwGen_FailedFormat"] = "调起密码生成器失败: {0}",
            ["Notification_CopiedPassword_Title"] = "已复制密码",
            ["Notification_CopiedPassword_Message"] = "条目「{0}」的密码已复制到剪贴板，原版倒计时保护已生效。",
            ["Notification_CopiedUsername_Title"] = "已复制用户名",
            ["Notification_CopiedUsername_Message"] = "条目「{0}」的用户名已复制到剪贴板。",
            ["Notification_CopiedUrl_Title"] = "已复制网址",
            ["Notification_CopiedUrl_Message"] = "条目「{0}」的网址已复制到剪贴板。",
            ["Notification_EntryDeleted_Title"] = "已删除条目",
            ["Notification_EntryDeleted_Message"] = "条目「{0}」已移至回收站。",
            ["Notification_RecycleBinEmptied_Title"] = "清空回收站",
            ["Notification_RecycleBinEmptied_Message"] = "回收站已成功清空。",
            ["Notification_SaveSuccess_Title"] = "保存成功",
            ["Notification_SaveSuccess_Message"] = "数据库已成功保存。",
            ["Notification_SaveFailed_Title"] = "保存失败",
            ["Notification_Locked_Title"] = "数据库已锁定",
            ["Notification_Locked_Message"] = "数据库已安全锁定。",
            ["Notification_RestoreSuccess_Title"] = "还原成功",
            ["Notification_RestoreSuccess_Message"] = "已成功还原至所选历史版本，当前状态已自动备份。",
            ["Notification_EntrySaved_Title"] = "保存成功",
            ["Notification_EntrySaved_Message"] = "条目「{0}」的修改已保存。",
            ["New_Entry_DefaultTitle"] = "新条目",
            ["Platform_NoticeMessage"] = "Fluent Pass 采用官方微软 Fluent Design System (WPF-UI)，深度依赖 Windows 11 DWMAPI 与 Direct3D 现代渲染管线。\n当前检测到操作系统为 {0} (Mono 运行时)，暂不支持 WPF-UI 硬件加速视窗。\n请在 Windows 10/11 环境下使用 Fluent 现代化视窗。",
            ["Platform_NoticeTitle"] = "Fluent Pass - 平台提示"
        };

        private static readonly Dictionary<string, string> EnStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Common & Navigation
            ["App_Title"] = "Fluent Pass",
            ["App_TitleLocked"] = "Fluent Pass (Locked)",
            ["Common_Save"] = "Save",
            ["Common_Cancel"] = "Cancel",
            ["Common_Edit"] = "Edit",
            ["Common_Delete"] = "Delete",
            ["Common_New"] = "New",
            ["Common_Restore"] = "Restore",
            ["Common_Close"] = "Close",
            ["Common_Settings"] = "Settings",
            ["Common_Prompt"] = "Notice",
            ["Common_Confirm"] = "Confirm",
            ["Common_Copy"] = "Copy",
            ["Common_Unknown"] = "Unknown",
            ["Common_Untitled"] = "(Untitled)",
            ["Common_UntitledEntry"] = "(Untitled Entry)",

            // TitleBar
            ["TitleBar_SaveTooltip"] = "Save database (Ctrl+S)",
            ["TitleBar_LockTooltip"] = "Lock database (Ctrl+L)",
            ["TitleBar_SearchPlaceholder"] = "Search entries (Ctrl+F)...",
            ["TitleBar_ToggleKeePassTooltip"] = "Toggle KeePass main window visibility",
            ["TitleBar_ThemeTooltip"] = "Switch appearance theme",
            ["TitleBar_ThemeDark"] = "Dark theme (Dark)",
            ["TitleBar_ThemeLight"] = "Light theme (Light)",
            ["TitleBar_ThemeSystem"] = "Follow system (System)",
            ["TitleBar_SettingsTooltip"] = "Settings (Ctrl+,)",

            // Database States
            ["DbState_LockedTitle"] = "Database is Locked",
            ["DbState_LockedPrompt"] = "To protect sensitive credentials, the database is locked.",
            ["DbState_UnlockButton"] = "Unlock Database Now",
            ["DbState_NotOpenTitle"] = "No Database Opened",
            ["DbState_NotOpenPrompt"] = "Please open or create a KDBX database in KeePass",
            ["DbState_OpenOriginalButton"] = "Open KeePass Window",

            // Groups
            ["Groups_Header"] = "Groups",
            ["Groups_EmptyRecycleBinTooltip"] = "Empty Recycle Bin",
            ["Groups_NewSubgroupTooltip"] = "New subgroup under selected group",
            ["Groups_DeleteGroupTooltip"] = "Delete current group",
            ["Groups_NewGroupName"] = "New Group",

            // Entries List
            ["Entries_SummaryFormat"] = "{0} entries ({1} shown)",
            ["Entries_NewButton"] = "New",
            ["Entries_EmptyTitle"] = "No Password Entries",
            ["Entries_EmptyPrompt"] = "No entries in current group. Click \"+ New\" above to create one.",
            ["Entries_ContextMenu_CopyPassword"] = "Copy Password",
            ["Entries_ContextMenu_CopyUsername"] = "Copy User Name",
            ["Entries_ContextMenu_CopyUrl"] = "Copy URL",
            ["Entries_ContextMenu_OpenInBrowser"] = "Open in Browser",
            ["Entries_ContextMenu_AutoType"] = "Perform Auto-Type",
            ["Entries_ContextMenu_EditEntry"] = "Edit Entry",
            ["Entries_ContextMenu_DeleteEntry"] = "Delete Entry",
            ["Entries_ContextMenu_OpenOriginal"] = "Open KeePass Entry Dialog",

            // Entry Detail
            ["Detail_NoSelectionTitle"] = "No Entry Selected",
            ["Detail_NoSelectionPrompt"] = "Select an entry from the list to view details",
            ["Detail_ModifiedFormat"] = "Modified {0}",
            ["Detail_TitlePlaceholder"] = "Entry Title",
            ["Detail_AutoTypeTooltip"] = "Perform global Auto-Type",
            ["Detail_OpenOriginalTooltip"] = "Open KeePass advanced entry dialog",
            ["Detail_UserNameLabel"] = "User Name",
            ["Detail_UserNameEmpty"] = "(No user name)",
            ["Detail_UserNamePlaceholder"] = "Enter user name",
            ["Detail_CopyUserNameTooltip"] = "Copy user name",
            ["Detail_PasswordLabel"] = "Password",
            ["Detail_PasswordAutoMaskFormat"] = "Auto-mask in {0}s",
            ["Detail_TogglePasswordTooltip"] = "Show/hide plain text (auto-masks in 10s)",
            ["Detail_CopyPasswordTooltip"] = "Copy password (clipboard auto-clears)",
            ["Detail_PasswordPlaceholder"] = "Enter password",
            ["Detail_ToggleEditPasswordTooltip"] = "Show/hide plain text",
            ["Detail_PwGenTooltip"] = "Open KeePass password generator",
            ["Detail_UrlLabel"] = "URL",
            ["Detail_UrlEmpty"] = "(No URL)",
            ["Detail_UrlPlaceholder"] = "https://...",
            ["Detail_OpenUrlTooltip"] = "Open in default browser",
            ["Detail_CopyUrlTooltip"] = "Copy URL",
            ["Detail_NotesHeader"] = "Notes",
            ["Detail_NotesEmpty"] = "(No notes)",
            ["Detail_NotesPlaceholder"] = "Enter notes here...",
            ["Detail_CustomFieldsHeader"] = "Custom Fields",
            ["Detail_CustomFieldsRevealTooltip"] = "Show plain text",
            ["Detail_AttachmentsHeader"] = "Attachments",
            ["Detail_AttachmentSaveTooltip"] = "Save attachment to disk",
            ["Detail_AttachmentViewTooltip"] = "View attachment in KeePass",
            ["Detail_HistoryHeader"] = "History Snapshots ({0})",
            ["Detail_HistoryTimeFormat"] = "Snapshot time: {0}",
            ["Detail_HistoryTitleFormat"] = "Entry title: {0}",
            ["Detail_HistoryRestoreTooltip"] = "Restore entry to this version (current state will be backed up)",
            ["Detail_CreatedFormat"] = "Created: {0}",
            ["Detail_LastModifiedFormat"] = "Last modified: {0}",

            // Password Quality
            ["PwQuality_NotSet"] = "Not set",
            ["PwQuality_NoPassword"] = "No password",
            ["PwQuality_VeryWeakFormat"] = "Very weak ({0} bits)",
            ["PwQuality_WeakFormat"] = "Weak ({0} bits)",
            ["PwQuality_ModerateFormat"] = "Moderate ({0} bits)",
            ["PwQuality_StrongFormat"] = "Strong ({0} bits)",

            // Settings View
            ["Settings_BackTooltip"] = "Return to workspace (Esc)",
            ["Settings_Title"] = "Settings",
            ["Settings_SectionBehavior"] = "Behavior & Integration",
            ["Settings_AutoTakeoverTitle"] = "Auto takeover KeePass at startup",
            ["Settings_AutoTakeoverDesc"] = "Automatically display Fluent window on KeePass startup and hide original window.",
            ["Settings_CloseActionTitle"] = "Window close behavior",
            ["Settings_CloseActionDesc"] = "Action to take when closing the Fluent window.",
            ["Settings_CloseAction_Restore"] = "Restore KeePass main window",
            ["Settings_CloseAction_Tray"] = "Minimize to system tray",
            ["Settings_CloseAction_Exit"] = "Exit KeePass",
            ["Settings_SectionAppearance"] = "Appearance & Personalization",
            ["Settings_ThemeTitle"] = "Appearance theme",
            ["Settings_ThemeDesc"] = "Choose between Dark, Light, or follow Windows 11 system theme.",
            ["Settings_Theme_System"] = "Follow system",
            ["Settings_Theme_Light"] = "Light theme",
            ["Settings_Theme_Dark"] = "Dark theme",
            ["Settings_LanguageTitle"] = "Display Language",
            ["Settings_LanguageDesc"] = "Select display language for Fluent Pass. Applies instantly.",
            ["Settings_Language_Auto"] = "Follow KeePass / System (Auto)",
            ["Settings_Language_Zh"] = "简体中文 (Chinese Simplified)",
            ["Settings_Language_En"] = "English",
            ["Settings_SectionSecurity"] = "Security & Protection",
            ["Settings_AutoMaskTitle"] = "Plain text auto-mask timeout",
            ["Settings_AutoMaskDesc"] = "Automatically re-mask plain text password after duration to prevent shoulder surfing.",
            ["Settings_Seconds5"] = "5 seconds",
            ["Settings_Seconds10Rec"] = "10 seconds (Recommended)",
            ["Settings_Seconds15"] = "15 seconds",
            ["Settings_Seconds30"] = "30 seconds",
            ["Settings_SectionAbout"] = "About",
            ["Settings_VersionFormat"] = "Version {0} · Built with Microsoft WPF-UI Fluent Design",
            ["Settings_ResetButton"] = "Reset to defaults",

            // Status Bar & Operations
            ["Status_Ready"] = "Ready",
            ["Status_ReadyUnsaved"] = "Ready (Unsaved changes)",
            ["Status_NotOpen"] = "No database opened",
            ["Status_UnnamedDb"] = "Unnamed database",
            ["Status_Locked"] = "Database locked",
            ["Status_LockedClickToUnlock"] = "Database is locked, please click to unlock",
            ["Status_Unlocked"] = "Database unlocked",
            ["Status_Saved"] = "Database saved",
            ["Status_SaveCancelled"] = "Save cancelled or not completed",
            ["Status_SaveFailedFormat"] = "Failed to save: {0}",
            ["Status_LockFailedFormat"] = "Failed to lock: {0}",
            ["Status_UnlockFailedFormat"] = "Failed to unlock: {0}",
            ["Status_ToggleWindowFailedFormat"] = "Failed to toggle window: {0}",
            ["Status_OriginalWindowHidden"] = "Original window hidden",
            ["Status_OriginalWindowShown"] = "Original window shown",
            ["Status_EntryCreated"] = "Entry created",
            ["Status_EntryCreateFailedFormat"] = "Failed to create entry: {0}",
            ["Status_EntryDeletedFormat"] = "Entry deleted: {0}",
            ["Status_EntryDeleteFailedFormat"] = "Failed to delete entry: {0}",
            ["Status_GroupCreated"] = "Group created",
            ["Status_GroupCreateFailedFormat"] = "Failed to create group: {0}",
            ["Status_GroupDeleted"] = "Group deleted",
            ["Status_GroupDeleteFailedFormat"] = "Failed to delete group: {0}",
            ["Status_RecycleBinEmptied"] = "Recycle Bin emptied",
            ["Status_RecycleBinEmptyFailedFormat"] = "Failed to empty Recycle Bin: {0}",
            ["Status_CopiedPasswordFormat"] = "Password copied: {0}",
            ["Status_CopiedPasswordFailedFormat"] = "Failed to copy password: {0}",
            ["Status_CopiedUsernameFormat"] = "User name copied: {0}",
            ["Status_CopiedUsernameFailedFormat"] = "Failed to copy user name: {0}",
            ["Status_CopiedUrlFormat"] = "URL copied: {0}",
            ["Status_CopiedUrlFailedFormat"] = "Failed to copy URL: {0}",
            ["Status_OpenUrlFailedFormat"] = "Failed to open URL: {0}",
            ["Status_AutoTypeFailedFormat"] = "Failed to perform Auto-Type: {0}",
            ["Status_OpenOriginalDialogFailedFormat"] = "Failed to open KeePass entry dialog: {0}",

            // Dialogs & Notifications
            ["Dialog_UnsavedChanges_Prompt"] = "The database has unsaved changes. Do you want to save now?",
            ["Dialog_UnsavedChanges_Title"] = "Unsaved Changes",
            ["Dialog_DeleteEntry_ConfirmFormat"] = "Are you sure you want to delete entry \"{0}\"?",
            ["Dialog_DeleteEntry_Title"] = "Confirm Delete",
            ["Dialog_RecycleBinAlreadyEmpty"] = "The Recycle Bin is already empty.",
            ["Dialog_EmptyRecycleBin_ConfirmFormat"] = "Are you sure you want to permanently delete all {0} items in the Recycle Bin?\nThis operation cannot be undone!",
            ["Dialog_EmptyRecycleBin_Title"] = "Empty Recycle Bin",
            ["Dialog_CannotDeleteRootGroup"] = "Cannot delete the root group!",
            ["Dialog_DeleteGroup_ConfirmFormat"] = "Are you sure you want to delete group \"{0}\" and all its entries?",
            ["Dialog_DeleteGroup_Title"] = "Confirm Delete Group",
            ["Dialog_RestoreHistory_ConfirmFormat"] = "Are you sure you want to restore this entry to the version from {0}?\nCurrent contents will be backed up as a new history snapshot.",
            ["Dialog_RestoreHistory_Title"] = "Confirm Restore History",
            ["Dialog_RestoreHistory_FailedFormat"] = "Failed to restore version: {0}",
            ["Dialog_SaveEntry_FailedFormat"] = "Failed to save changes: {0}",
            ["Dialog_PwGen_FailedFormat"] = "Failed to open password generator: {0}",
            ["Notification_CopiedPassword_Title"] = "Password Copied",
            ["Notification_CopiedPassword_Message"] = "Password for \"{0}\" copied to clipboard. Auto-clear timer active.",
            ["Notification_CopiedUsername_Title"] = "User Name Copied",
            ["Notification_CopiedUsername_Message"] = "User name for \"{0}\" copied to clipboard.",
            ["Notification_CopiedUrl_Title"] = "URL Copied",
            ["Notification_CopiedUrl_Message"] = "URL for \"{0}\" copied to clipboard.",
            ["Notification_EntryDeleted_Title"] = "Entry Deleted",
            ["Notification_EntryDeleted_Message"] = "Entry \"{0}\" moved to Recycle Bin.",
            ["Notification_RecycleBinEmptied_Title"] = "Recycle Bin Emptied",
            ["Notification_RecycleBinEmptied_Message"] = "Recycle Bin was successfully emptied.",
            ["Notification_SaveSuccess_Title"] = "Save Successful",
            ["Notification_SaveSuccess_Message"] = "Database was saved successfully.",
            ["Notification_SaveFailed_Title"] = "Save Failed",
            ["Notification_Locked_Title"] = "Database Locked",
            ["Notification_Locked_Message"] = "Database was safely locked.",
            ["Notification_RestoreSuccess_Title"] = "Restore Successful",
            ["Notification_RestoreSuccess_Message"] = "Successfully restored to selected history version. Current state backed up.",
            ["Notification_EntrySaved_Title"] = "Save Successful",
            ["Notification_EntrySaved_Message"] = "Changes to entry \"{0}\" were saved.",
            ["New_Entry_DefaultTitle"] = "New Entry",
            ["Platform_NoticeMessage"] = "Fluent Pass uses Microsoft Fluent Design System (WPF-UI) which relies on Windows DWMAPI and Direct3D.\nDetected OS is {0} (Mono runtime), which does not support WPF-UI hardware acceleration.\nPlease use Fluent Pass on Windows 10/11.",
            ["Platform_NoticeTitle"] = "Fluent Pass - Platform Notice"
        };

        public static IReadOnlyDictionary<string, string> GetDictionary(string languageCode)
        {
            if (string.Equals(languageCode, "en-US", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase))
            {
                return EnStrings;
            }

            return ZhStrings;
        }

        public static string Get(string languageCode, string key)
        {
            var dict = GetDictionary(languageCode);
            if (dict.TryGetValue(key, out var val))
            {
                return val;
            }
            var fallback = GetFallbackDictionary(languageCode);
            if (fallback.TryGetValue(key, out var fallbackVal))
            {
                return fallbackVal;
            }
            return key;
        }

        public static IReadOnlyDictionary<string, string> GetFallbackDictionary(string languageCode)
        {
            if (string.Equals(languageCode, "en-US", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase))
            {
                return ZhStrings;
            }

            return EnStrings;
        }

        public static IEnumerable<string> GetAllKeys()
        {
            return ZhStrings.Keys;
        }
    }
}
