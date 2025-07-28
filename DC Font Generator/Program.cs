using System;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.Versioning;
using System.Text; // 添加这个命名空间

[assembly: SupportedOSPlatform("windows")]

namespace DC_Font_Generator
{
	static class Program
	{
		public static Font DefaultFont { get; private set; } = SystemFonts.DefaultFont;

		[STAThread]
		[SupportedOSPlatform("windows")]
		static void Main()
		{
			// 🔴 关键修复：注册编码提供程序（必须放在应用程序启动的第一个位置）
			RegisterEncodingProviders();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// 检查.NET版本 - 这个检查可能需要更新
			if (Environment.Version.Major < 6) // 针对.NET Core 6+的更新检查
			{
				MessageBox.Show(".NET 6.0 or later is required.", ".NET Version Requirement",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			// 设置全局默认字体
			SetDefaultFont(new Font("Microsoft YaHei UI", 10f));

			Application.Run(new MainForm());
		}

		/// <summary>
		/// 注册所有必需的编码提供程序
		/// </summary>
		private static void RegisterEncodingProviders()
		{
			try
			{
				// 必须注册CodePagesEncodingProvider才能支持932,936,949,950等编码
				Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

				Console.WriteLine("Encoding providers registered successfully.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Failed to register encoding providers:\n{ex.Message}",
							  "Critical Error",
							  MessageBoxButtons.OK,
							  MessageBoxIcon.Error);

				// 如果无法注册，尝试强制关闭
				Environment.Exit(1);
			}
		}

		[SupportedOSPlatform("windows")]
		public static void SetDefaultFont(Font font)
		{
			if (font == null)
				throw new ArgumentNullException(nameof(font));

			// 先释放旧字体
			DefaultFont?.Dispose();

			// 更新全局字体
			DefaultFont = font;

			// 递归设置字体
			foreach (Form form in Application.OpenForms)
			{
				SetControlFontRecursive(form, font);
			}

			// 订阅事件
			if (!_eventSubscribed)
			{
				Application.ApplicationExit += (s, args) => {
					DefaultFont?.Dispose();
					DefaultFont = null;
				};
				_eventSubscribed = true;
			}
		}

		private static bool _eventSubscribed = false;

		/// <summary>
		/// 设置控件及其所有子控件的字体
		/// </summary>
		[SupportedOSPlatform("windows")]
		public static void SetControlFontRecursive(Control control, Font font)
		{
			if (control == null) return;

			// 对某些特殊控件进行例外处理
			if (!(control is DataGridView) &&
				!(control is PropertyGrid) &&
				!(control is ToolStrip))
			{
				control.Font = font;
			}

			foreach (Control child in control.Controls)
			{
				SetControlFontRecursive(child, font);
			}
		}
	}

	// 基础窗体类
	[SupportedOSPlatform("windows")]
	public class BaseForm : Form
	{
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (Program.DefaultFont != null)
			{
				Program.SetControlFontRecursive(this, Program.DefaultFont);
			}
		}
	}
}