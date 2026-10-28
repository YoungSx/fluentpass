using System;
using System.Collections.Generic;
using KeePass;
using KeePass.App.Configuration;
using KeePassLib;
using KeePassLib.Utility;

namespace FluentPass.Services
{
    /// <summary>
    /// 提供用户名自动建议能力，严格对齐 KeePass 原生 PwEntryForm.InitUserNameSuggestions 行为规范：
    /// 1. 若配置中启用了主界面用户名星号遮蔽（HideWithAsterisks），禁用自动建议以防止隐私泄露。
    /// 2. 收集数据库中所有非空用户名（包含数据库默认用户名 DefaultUserName）。
    /// 3. 使用 KeePass 原生 StrUtil.CaseIgnoreComparer 进行去重与排序。
    /// </summary>
    public static class UserNameSuggestionService
    {
        /// <summary>
        /// 从数据库中获取所有已排序去重的候选用户名列表。
        /// </summary>
        /// <param name="db">目标数据库</param>
        /// <param name="checkAsterisksMask">是否检查主窗口列星号遮蔽配置（默认 true）</param>
        /// <returns>去重并排序后的用户名列表</returns>
        public static List<string> GetSuggestions(PwDatabase? db, bool checkAsterisksMask = true)
        {
            if (checkAsterisksMask && IsUserNameMaskedWithAsterisks())
            {
                return new List<string>();
            }

            if (db == null || !db.IsOpen || db.RootGroup == null)
            {
                return new List<string>();
            }

            var uniqueUsers = new Dictionary<string, bool>(StrUtil.CaseIgnoreComparer);

            // 数据库默认用户名（若有设置）
            string defaultUser = db.DefaultUserName?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(defaultUser))
            {
                uniqueUsers[defaultUser] = true;
            }

            // 递归遍历整棵条目树收集条目用户名
            try
            {
                db.RootGroup.TraverseTree(TraversalMethod.PreOrder, null, pe =>
                {
                    if (pe != null)
                    {
                        string u = pe.Strings.ReadSafe(PwDefs.UserNameField)?.Trim() ?? string.Empty;
                        if (!string.IsNullOrEmpty(u))
                        {
                            uniqueUsers[u] = true;
                        }
                    }
                    return true;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UserNameSuggestionService traversal error: {ex.Message}");
            }

            var result = new List<string>(uniqueUsers.Keys);
            result.Sort(StrUtil.CaseIgnoreComparer);
            return result;
        }

        /// <summary>
        /// 检查 KeePass 配置中是否启用了主窗口用户名列的星号遮蔽（HideWithAsterisks）。
        /// </summary>
        public static bool IsUserNameMaskedWithAsterisks()
        {
            try
            {
                var col = Program.Config?.MainWindow?.FindColumn(AceColumnType.UserName);
                return col != null && col.HideWithAsterisks;
            }
            catch
            {
                return false;
            }
        }
    }
}
