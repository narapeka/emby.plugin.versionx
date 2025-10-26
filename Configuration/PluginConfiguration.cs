using System.ComponentModel;
using Emby.Web.GenericEdit;

namespace EmbyVersionByFolder.Configuration
{
    public class PluginConfiguration : EditableOptionsBase
    {
        public override string EditorTitle => "文件夹版本命名";

        public override string EditorDescription => "根据父文件夹路径差异自动设置版本名称。\n"
                                                    + "此插件通过使用文件夹名称作为版本标识符，解决文件名相同时默认版本名称不明确的问题。\n\n";

        [DisplayName("启用插件")]
        [Description("启用基于文件夹差异的自动版本命名功能")]
        public bool Enabled { get; set; } = true;
    }
}
