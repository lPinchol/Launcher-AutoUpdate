﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace SharpUpdate
{
    /// <summary>
    /// Form that download the update
    /// </summary>
    internal partial class SharpUpdateDownloadForm : Form
    {
        /// <summary>
        /// The web client to download the update
        /// </summary>
        private WebClient webClient;

        /// <summary>
        /// The thread to hash the file
        /// </summary>
        private BackgroundWorker bgWorker;

        /// <summary>
        /// A temp file name to download to
        /// </summary>
        private string tempFile;

        /// <summary>
        /// The MD5 hash of the file to download
        /// </summary>
        private string md5;

        /// <summary>
        /// Gets the temp file path fot the download file
        /// </summary>
        internal string TempFilePatch
        {
            get { return this.tempFile;}
        }

        /// <summary>
        /// Creates a new SharpUpdateDownloadForm
        /// </summary>
        internal SharpUpdateDownloadForm(Uri location,string md5,Icon programIcon)
        {
            InitializeComponent();

            if (programIcon != null)
            {
                this.Icon = programIcon;
            }

            tempFile = Path.GetTempFileName();

            this.md5 = md5;

            webClient = new WebClient();
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(webClient_DownloadProgressChanged);
            webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(webClient_DownloadFileCompleted);

            bgWorker = new BackgroundWorker();
            bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
            bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            try { webClient.DownloadFileAsync(location, this.tempFile); }
            catch { this.DialogResult = DialogResult.No; this.Close(); }
        }

        private void webClient_DownloadProgressChanged(Object sender, DownloadProgressChangedEventArgs e)
        {
            this.progressBar.Value = e.ProgressPercentage;
            this.lblProgress.Text = String.Format("Download {0} of {1}",FormatByte(e.BytesReceived,1,true),FormatByte(e.TotalBytesToReceive,1,true));
        }

        private void webClient_DownloadFileCompleted(Object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                this.DialogResult = DialogResult.No;
                this.Close();
            }
            else if (e.Cancelled)
            {
                this.DialogResult = DialogResult.Abort;
                this.Close();
            }
            else
            {
                lblProgress.Text = "Verifying Download ...";
                progressBar.Style = ProgressBarStyle.Marquee;

                bgWorker.RunWorkerAsync(new string[] {this.tempFile,this.md5});
            }
        }

        private void bgWorker_DoWork(Object sender, DoWorkEventArgs e)
        {
            string file = ((string[])e.Argument)[0];
            string updateMd5 = ((string[])e.Argument)[1];

            if (Hasher.HashFile(file, HashType.MD5) != updateMd5)
            {
                e.Result = DialogResult.No;
            }
            else
            {
                e.Result = DialogResult.OK;
            }
        }

        private void bgWorker_RunWorkerCompleted(Object sender, RunWorkerCompletedEventArgs e)
        {
            this.DialogResult = (DialogResult)e.Result;
            this.Close();
        }

        private string FormatByte(long bytes, int decimalPlaces, bool showByteType)
        {
            double newBytes = bytes;
            string formatString = "{0";
            string byteType = "8";

            if (newBytes > 1024 && newBytes < 1048576)
            {
                newBytes /= 1024;
                byteType = "KB";
            }
            else if (newBytes > 1048576 && newBytes < 1073741824)
            {
                newBytes /= 1048576;
                byteType = "MB";
            }
            else
            {
                newBytes /= 1073741824;
                byteType = "GB";
            }

            if (decimalPlaces > 0)
            {
                formatString += ":0.";
            }

            for (int i = 0; i < decimalPlaces; i++)
            {
                formatString += "0";
            }

            formatString += "}";

            if (showByteType)
            {
                formatString += byteType;
            }

            return string.Format(formatString, newBytes);
        }

        private void SharpUpdateDownloadForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (webClient.IsBusy)
            {
                webClient.CancelAsync();
                this.DialogResult = DialogResult.Abort;
            }

            if (bgWorker.IsBusy)
            {
                bgWorker.CancelAsync();
                this.DialogResult = DialogResult.Abort;
            }
        }
    }
}
