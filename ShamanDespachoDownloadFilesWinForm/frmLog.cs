using System;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Configuration;
using System.Data.SqlClient;

namespace ShamanDespachoDownloadFilesWinForm
{
    public partial class frmLog : Form
    {
        string dBServer1 = ConfigurationManager.AppSettings["DBServer1"];
        public frmLog()
        {
            InitializeComponent();
            this.tmrRefresh.Enabled = true;
            this.tmrRefresh_Tick(null, null);
        }

        private void addLog(bool rdo, string logProcedure, string logDescription, bool clear = false)
        {

            string path;

            path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            path = path + "\\" + DateTime.Now.ToString("yyyy-MM-dd").Replace("-", "_") + ".log";

            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Log " + DateTime.Now.Date);
                    this.txtLog.Text = "Log " + DateTime.Now.Date + Environment.NewLine;
                }
            }

            using (StreamWriter sw = File.AppendText(path))
            {
                string rdoStr = "Ok";
                if (rdo == false)
                {
                    rdoStr = "Error";
                }

                sw.WriteLine(DateTime.Now.Hour.ToString("00") + ":" + DateTime.Now.Minute.ToString("00") + "\t" + rdoStr + "\t" + logProcedure + "\t" + logDescription);
                if (clear) { this.txtLog.Text = ""; }
                this.txtLog.Text = this.txtLog.Text + DateTime.Now.Hour.ToString("00") + ":" + DateTime.Now.Minute.ToString("00") + "\t" + rdoStr + "\t" + logProcedure + "\t" + logDescription + Environment.NewLine;

            }

        }

        private void tmrRefresh_Tick(object sender, System.EventArgs e)
        {
            this.tmrRefresh.Enabled = false;

            /*------> Proceso <--------*/
            this.DownladFile();

            this.tmrRefresh.Enabled = true;
        }

        #region proceso

        public void DownladFile()
        {
            string queryString = "SELECT inc.id, inc.Url, clf.RutaRemota + '\' + CAST(inc.IncidenteId as varchar) + '_' + cast(inc.ID as varchar) + '.' + ";
            queryString += "SUBSTRING( RIGHT(inc.Url, 4), CHARINDEX('.', RIGHT(inc.Url, 4)) + 1, LEN(RIGHT(inc.Url, 4)) - CHARINDEX('.', RIGHT(inc.Url, 4)) ) as FTP ";
            queryString += "FROM IncidentesAdjuntos inc INNER JOIN AdjuntosClasificaciones clf ON inc.AdjuntoClasificacionId = clf.ID ";
            queryString += "WHERE inc.flgDescargado = 0 AND inc.Url IS NOT NULL";

            using (SqlConnection connection = new SqlConnection(dBServer1))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {

                    while (reader.Read())
                    {
                        string incId = reader["id"].ToString();
                        string urlOrigin = reader["Url"].ToString();
                        string ftpSource = reader["FTP"].ToString();

                        //urlOrigin = "https://www.marketingdirecto.com/wp-content/uploads/2019/10/logo-volkswagen.jpg";
                        //ftpSource = @"\\archivos02\ftp paramedic\carpeta\image35.png";

                        if (!Directory.Exists(Path.GetDirectoryName(ftpSource)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(ftpSource));
                        }

                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(new Uri(urlOrigin), @ftpSource);

                        }
                        if (!updateStatus(incId, 1))
                            if (!updateStatus(incId, 1))
                                throw new Exception("No se puede actualizar el estado de flgDescargado.");
                    }
                }
                catch (Exception ex)
                {
                    addLog(false, "DownloadFile", ex.Message);
                }
                finally
                {
                    // Always call Close when done reading.
                    reader.Close();
                }
            }
        }

        private bool updateStatus(string incId, int flgDescargado)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(dBServer1))
                {
                    string queryString = "UPDATE IncidentesAdjuntos SET flgDescargado = " + flgDescargado + " WHERE id = " + incId;
                    SqlCommand commandUpdate = new SqlCommand(queryString, connection);
                    connection.Open();
                    return Convert.ToBoolean(commandUpdate.ExecuteNonQuery());
                }
            }
            catch (Exception ex)
            {
                addLog(false, "updateStatus", ex.Message);
            }
            return false;
        }

        #endregion
    }
}
