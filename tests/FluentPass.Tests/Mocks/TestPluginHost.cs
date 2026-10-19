using System.Resources;
using KeePass.App.Configuration;
using KeePass.DataExchange;
using KeePass.Ecas;
using KeePass.Forms;
using KeePass.Plugins;
using KeePass.UI;
using KeePass.Util;
using KeePassLib;
using KeePassLib.Cryptography.Cipher;
using KeePassLib.Cryptography.PasswordGenerator;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

namespace FluentPass.Tests.Mocks
{
    public class TestPluginHost : IPluginHost
    {
        public MainForm? MainWindow { get; set; }
        public PwDatabase Database { get; set; }

        public CommandLineArgs CommandLineArgs => null!;
        public AceCustomConfig CustomConfig { get; } = new AceCustomConfig();
        public CipherPool CipherPool => null!;
        public KeyProviderPool KeyProviderPool => null!;
        public KeyValidatorPool KeyValidatorPool => null!;
        public FileFormatPool FileFormatPool => null!;
        public TempFilesPool TempFilesPool => null!;
        public EcasPool EcasPool => null!;
        public EcasTriggerSystem TriggerSystem => null!;
        public CustomPwGeneratorPool PwGeneratorPool => null!;
        public ColumnProviderPool ColumnProviderPool => null!;
        public ResourceManager Resources => null!;

        public TestPluginHost()
        {
            Database = new PwDatabase();
            var key = new CompositeKey();
            key.AddUserKey(new KcpPassword("TestMasterPassword"));
            Database.New(new IOConnectionInfo(), key);
        }

        public static PwEntry CreateTestEntry(string title, string username, string password, string url = "", string notes = "")
        {
            var entry = new PwEntry(true, true);
            entry.Strings.Set(PwDefs.TitleField, new ProtectedString(false, title));
            entry.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, username));
            entry.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, password));
            entry.Strings.Set(PwDefs.UrlField, new ProtectedString(false, url));
            entry.Strings.Set(PwDefs.NotesField, new ProtectedString(false, notes));
            return entry;
        }
    }
}
