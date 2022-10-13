using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _Logger
{


    public class LogFile
    {
        public bool LogEnabled
        {
            get { return this.enabled; }
            set { this.enabled = value; }
        }

        private string logPath = Application.StartupPath + @"\Log.txt";
        private int maxKbFileSize = 5120; //tamanho por defeito do ficheiro é 5mb
        private bool enabled = false;
        private object locker = new object();

        /// <summary>
        /// Tamanho atual do ficheiro, em kb
        /// </summary>
        public int FileSize
        {
            get { return this.GetFileSize(this.logPath); }
        }

        public string Filename { get { return this.logPath; } }

        public bool DbInsert { get; private set; } = false;



        public LogFile(string _logPath = "", bool dbInsert = false, int _maxKbFileSize = 5120, bool _enabled = true)
        {
            if (string.IsNullOrWhiteSpace(_logPath))
                logPath = Application.StartupPath + @"\Logs\Log.txt";
            else
                logPath = _logPath;

            maxKbFileSize = _maxKbFileSize;

            this.DbInsert = dbInsert;

            this.enabled = _enabled;

            //Cria o diretório, caso o mesmo não exista
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

            if (!File.Exists(logPath))
                File.Create(logPath);

            //Força o coletor de memória
            GC.Collect();
        }

        /// <summary>
        /// Adiciona uma nova linha no ficheiro de log
        /// </summary>
        /// <param name="text">Texto a escrever. Não é necessário inserir quebra de linha!</param>
        /// <param name="includeDatetime">Incluir timestamp no inicio da linha?</param>
        public void WriteLine(string text, DateTime dtRegisto, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
        {
            if (this.enabled)
            {
                lock (this.locker)
                    try
                    {
                        if (File.Exists(logPath))
                            this.CheckFileSize(logPath, maxKbFileSize);

                        //Gravar no ficheiro o texto
                        File.AppendAllText(logPath, "[" + dtRegisto.ToString("dd/MM/yy HH:mm:ss") + "] [L: " + lineNumber + "] - Member: '" + memberName + "' | Filename: '" + Path.GetFileName(fileName) + "' | Msg: " + text + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("LogFile.WriteLine(): " + ex.Message);
                    }

                //insere o log na base de dados em uma thread
                if (this.DbInsert)
                    Task.Run(() => { this.InsertIntoDB(text.Replace("'", ""), dtRegisto, memberName, Path.GetFileName(fileName), lineNumber); });
            }
        }


        private bool InsertIntoDB(string msg, DateTime datahora, string memberName, string fileName, int lineNumber)
        {
            return SQLiteHelper.ExecuteNonQuery("INSERT INTO LOGS (Text, MemberName, Filename, LineNumber, Datetime) VALUES ('" + msg + "','" + memberName + "','" + fileName + "'," + lineNumber + ",'" + SQLiteHelper.FormataDateTime(datahora) + "')", Variaveis.DatabaseConnectionString) == 1;
        }

        /// <summary>
        /// Verifica o tamanho do ficheiro e caso tenha chegado ao setpoint de tamanho cria um ficheiro antigo com os dados e esvazia o atual.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="limitSize"></param>
        private void CheckFileSize(string filePath, int limitSize)
        {
            //Caso o tamanho do ficheiro seja igual ou superior ao SP para o ficheiro
            if (this.GetFileSize(filePath) >= limitSize)
            {
                string newFileName = Path.GetFileNameWithoutExtension(filePath) + "_" + Convert.ToString(Diversos.ObterTempoUnixAtual()) + Path.GetExtension(filePath);

                //Verificar que já não existe um ficheiro criado com o mesmo nome
                if (File.Exists(newFileName))
                    File.Delete(newFileName);

                //Vamos mover o conteudo do ficheiro antigo para um novo ficheiro
                File.Move(filePath, newFileName);

                Debug.WriteLine("Tamanho máximo do ficheiro excedido! Ficheiro movido com o seguinte filename: " + newFileName);
            }
        }

        /// <summary>
        /// Obtem o tamanho do ficheiro, em kb
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private int GetFileSize(string filePath)
        {
            return Convert.ToInt32(new FileInfo(filePath).Length / 1024.0);
        }

        public void AttachButtonLogging(Form form)
        {
            foreach (var field in form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.GetValue(form) is Button)
                {
                    System.Windows.Forms.Button button = (Button)field.GetValue(form);
                    button.Click += LogButtonClick;
                }
            }
        }
        public void AttachButtonLogging(Control form)
        {
            foreach (var field in form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.GetValue(form) is Button)
                {
                    System.Windows.Forms.Button button = (Button)field.GetValue(form);
                    button.Click += LogButtonClick;
                }
            }
        }

        private void LogButtonClick(object sender, EventArgs eventArgs)
        {
            Button button = sender as Button;
            WriteLine("Button Click - " + button.FindForm().Name + "." + button.Name.ToString() + " Button Text: '" + button.Text + "'", DateTime.Now);
        }
    }

}
