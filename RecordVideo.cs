using _22039AI_BIN_PICKING;
using Accord.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Outros
{
    public class RecordVideo
    {

        private VideoFileWriter writer = new VideoFileWriter();

        public bool IsEnabled { get; private set; } = false;
        public string MainFolder { get; private set; } = @"C:\STREAK\Viewer\";

        public DateTime StartVideoDt { get; private set; } = DateTime.MinValue;
        public TimeSpan SetPointVideo { get; private set; } = TimeSpan.FromMinutes(10);

        public Size Resolution { get; private set; } = new Size(1920, 1080);
        public int Framerate { get; private set; } = 25;

        public bool IsRecording
        {
            get
            {
                return this.writer != null && this.writer.IsOpen;
            }
        }

        public RecordVideo(string iniFile)
        {
            using (FicheiroINI ini = new FicheiroINI(iniFile))
            {
                this.IsEnabled = ini.RetornaTrueFalseDeStringFicheiroINI("RecordVideo", "IsEnabled", this.IsEnabled);
                this.MainFolder = ini.RetornaINI("RecordVideo", "MainFolder", this.MainFolder);
                this.SetPointVideo = TimeSpan.FromSeconds(Convert.ToInt64(ini.RetornaINI("RecordVideo", "SetPointVideo", this.SetPointVideo.TotalSeconds.ToString("0"))));
                this.Resolution = new Size(
                   Convert.ToInt32(ini.RetornaINI("RecordVideo", "ResolutionX", this.Resolution.Width.ToString())),
                   Convert.ToInt32(ini.RetornaINI("RecordVideo", "ResolutionY", this.Resolution.Height.ToString()))
                   );
                this.Framerate = Convert.ToInt32(ini.RetornaINI("RecordVideo", "Framerate", this.Framerate.ToString()));

            }
        }

        public bool AddFrame(Bitmap frame)
        {
            try
            {
                if (!this.IsEnabled)
                {
                    //se desabilitado e write ativo, desliga-o
                    if (this.writer != null && this.writer.IsOpen)
                        this.Close();

                    return true;
                }

                //save to file
                if (this.writer == null || !this.writer.IsOpen)
                {
                    this.writer = new VideoFileWriter();

                    string folder = this.MainFolder + DateTime.Now.ToString("yyyyMMdd") + @"\";

                    //cria a diretoria se nao existir
                    Directory.CreateDirectory(folder);

                    //abre o streamer
                    this.writer.Open(folder + DateTime.Now.ToString("yyyy_MM_dd-HH_mm_ss") + ".avi", this.Resolution.Width, this.Resolution.Height, this.Framerate, VideoCodec.MPEG4);

                    //guarda a data inicial do video
                    this.StartVideoDt = DateTime.Now;
                }

                //se o writer estiver aberto vamos adicionar um frame ao video
                if (this.writer != null && writer.IsOpen)
                {
                    TimeSpan ts = DateTime.Now - this.StartVideoDt;
                    this.writer.WriteVideoFrame(frame, ts);

                    //se lenght do video ultrapssar o setpoint fecha o stream
                    if (ts >= this.SetPointVideo)
                        this.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RecordVideo - AddFrame(): " + ex.Message);

                if (writer != null)
                    writer.Close();

                return false;
            }
        }

        private void Close()
        {
            if (this.writer != null)
                this.writer.Close();

            this.writer.Dispose();

            this.writer = null;
        }
    }

}
