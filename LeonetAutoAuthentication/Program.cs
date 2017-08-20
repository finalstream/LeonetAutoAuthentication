using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32.TaskScheduler;

namespace LeonetAutoAuthentication
{
    static class Program
    {
        public const string WindowsLAATaskName = "Leonet Auto Authentication Startup";
        public static bool isReset = false;
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            //UnhandledExceptionイベントハンドラを追加する
            System.AppDomain.CurrentDomain.UnhandledException +=
                new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            if (args.Length > 0)
            {
                if ("/reset".Equals(args[0]))
                {
                    isReset = true;
                }

                string leonetlink = System.Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\leonet.lnk";

                if ("/regist".Equals(args[0]))
                {
                    // Check to make sure account privileges allow task deletion
                    if (!IsInAdminRole())
                    {
                        MessageBox.Show("権限が不足しております。管理者として実行してください。");
                        Environment.Exit(0);
                    }

                    var result = CreateWindowsTask(Application.ExecutablePath);

                    if (result)
                    {
                        MessageBox.Show("スタートアップに登録しました。");
                    }

                    Environment.Exit(0);
                }

                if ("/unregist".Equals(args[0]))
                {
                    // 旧バージョンの可能性があるのでショートカット削除
                    if (File.Exists(leonetlink))
                    {
                        File.Delete(leonetlink);
                        MessageBox.Show("旧スタートアップを解除しました。"); 
                    }

                    // タスクスケジューラ削除
                    // Check to make sure account privileges allow task deletion
                    if (!IsInAdminRole())
                    {
                        MessageBox.Show("権限が不足しております。管理者として実行してください。");
                        Environment.Exit(0);
                    }
                    using (var ts = new TaskService())
                    {
                        var t = ts.GetTask(WindowsLAATaskName);

                        if (t != null)
                        {
                            
                            // Remove the task we just created
                            ts.RootFolder.DeleteTask(WindowsLAATaskName);
                            MessageBox.Show("スタートアップを解除しました。");
                        }
                    }

                    Environment.Exit(0);
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                //エラーメッセージを表示する
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                //アプリケーションを終了する
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// 管理者ロールを保持しているか確認する
        /// </summary>
        /// <returns></returns>
        private static bool IsInAdminRole()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool CreateWindowsTask(string executablePath)
        {
            using (var ts = new TaskService())
            {
                var t = ts.GetTask(WindowsLAATaskName);

                if (t != null) return false; // タスクがすでに存在する

                // Create a new task definition and assign properties
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Description = "Windows起動時にLeonetに接続します。";
                td.Principal.LogonType = TaskLogonType.S4U;

                var bt = new BootTrigger();
                bt.Delay = TimeSpan.FromSeconds(10);
                td.Triggers.Add(bt);

                td.Actions.Add(new ExecAction(executablePath));

                td.Settings.DisallowStartIfOnBatteries = false;

                // Register the task in the root folder
                ts.RootFolder.RegisterTaskDefinition(WindowsLAATaskName, td);

                return true;
            }
        }

        
    }
}
