using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using FastSolver;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using System.Drawing.Imaging;
using OpenQA.Selenium.Support.UI;
using bestcaptchasolver;
using Xunit;

namespace Selenium_Project
{
    public partial class Form1 : Form
    {
        public string server_Id;
        public string server_Uid;
        bool work_state = false;
        Thread work_Thread;
        ChromeDriver driver;
        ChromeDriver driver2;
        Random rnd = new Random();
        string Fid = "";
        string Fps = "";
        string ACCESS_TOKEN = "";

        int id_Success = 1;
        int id_Failed = 1;
        int ip_C = 0;
        int id_donot = 0;

        private string expireDateString;
        public Form1(string expireDate)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Find_Fid();
            Get_Point();
            Check_Point();
            expireDateString = expireDate;
            ExpireDate.Text = expireDateString;
        }
        private void Form1_FormClosing(object sender, FormClosedEventArgs e)
        {
            this.Hide();
            try
            {
                driver.Quit();
            }
            catch { }
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void 백업데이터_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int i = 0; i < 백업데이터.Rows.Count; i++)
            {
                // 행 증가 시켜줌
                백업데이터.Rows[i].Cells[0].Value = (i + 1);
                if (Convert.ToString(백업데이터.Rows[i].Cells[6].Value) == "")
                {
                    백업데이터.Rows[i].Cells[6].Value = "대기";
                }
            }
        }
        private void 백업데이터(object sender, DataGridViewRowsRemovedEventArgs e) // RowsAdded랑 코드 같지만 수정할 필요는 없음
        {
            for (int i = 0; i< 백업데이터.Rows.Count; i++)
            {
                백업데이터.Rows[i].Cells[0].Value = (i + 1);
                if (Convert.ToString(백업데이터.Rows[i].Cells[6].Value) == "")
                {
                    백업데이터.Rows[i].Cells[6].Value = "대기";
                }
            }
        }
        private void 게시시작_Click(object sender, EventArgs e)
        {
            // 게시시작을 눌렀을 때의 메소드이므로 게시시작을 다시 눌렀을 때 반응이 없도록 지정
            게시시작.Enabled = false;
            일시정지.Enabled = true;

            // 백업 작업
            try
            {
                //중복저장 피하기 위해 파일이 이미 존재하면 삭제
                if (File.Exists(Application.StartupPath + "\\백업.txt")) 
                {
                    File.Delete(Application.StartupPath + "\\백업.txt");
                }
                for (int i = 0; i < 백업데이터.Rows.Count - 1; i++)
                {
                    FileStream fs = new FileStream(Application.StartupPath + "\\백업.txt", FileMode.Append, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                    sw.WriteLine(Convert.ToBoolean(백업데이터.Rows[i].Cells[0].Value) + "\t" + Convert.ToString(백업데이터.Rows[i].Cells[1].Value) + "\t" + Convert.ToString(백업데이터.Rows[i].Cells[2].Value) + "\t" + Convert.ToString(백업데이터.Rows[i].Cells[3].Value) + "\t" + Convert.ToString(백업데이터.Rows[i].Cells[4].Value) + "\t" + Convert.ToString(백업데이터.Rows[i].Cells[5].Value));
                    sw.Close();
                    fs.Close();
                }
            }
            catch { }

            if (work_state == true)
            {
                work_Thread.Resume();
            }
            else
            {
                work_Thread = new Thread(new ThreadStart(Web_Thread));
                work_Thread.SetApartmentState(ApartmentState.STA);
                work_Thread.Start();
            }
        }
        private void 일시정지_Click(object sender, EventArgs e)
        {
            // 일시정지를 누른 시점이므로 일시정지를 다시 눌러도 재시작되는 일이 없도록 설정
            게시시작.Enabled = true;
            일시정지.Enabled = false;

            // 진행중인 스레드 일시중지
            work_Thread.Suspend();
            // 일시중지 이므로 작동상태는 true 설정
            work_state = true;
        }
        void Web_Thread()
        {
            // 게시시작을 눌렀을때 작동중이지 않아서 새로 생성하는 스레드
            for (int j = 0; j < 백업데이터.Rows.Count - 1; j++)
            {
                // 쓰지 않은 아디 0
                id_donot = 0;

                // 크롬창 두개 생성
                Create_Chrome();
                Create_Chrome2();
                
               LOGIN(~) //로그인 과정 및 게시 과정

                // 크롬창 종료
                Quit_Chrome();
                Quit_Chrome2();

                // 다시 백업
                FileStream fs = new FileStream(Application.StartupPath + "\\백업.txt", FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                sw.WriteLine(Convert.ToString(백업데이터.Rows[j].Cells[0].Value));
                sw.Close();
                fs.Close();

                try
                {
                    // 확인차 다시 종료
                    Quit_Chrome();
                    Quit_Chrome2();
                }
                catch { }
            }
            // 크롬 창까지 종료 후 메세지 띄우기
            MessageBox.Show("모든 글쓰기 작업완료");

            // 작업을 모두 마쳤으니 다시 게시시작을 누르면 작업을 하도록 설정
            게시시작.Enabled = true;
            일시정지.Enabled = false;

            // 작동 상태는 false.
            work_state = false;
        }
       
        void Article_Paste()
        {
            Thread.Sleep(300);

            for (int i = 0; i < 5; i++)
            {
                // 전부 복사
                driver2.FindElement(By.CssSelector("body")).SendKeys(OpenQA.Selenium.Keys.Control + "a");
                Thread.Sleep(500);
                driver2.FindElement(By.CssSelector("body")).SendKeys(OpenQA.Selenium.Keys.Control + "c");
                Thread.Sleep(500);
            }
            Thread.Sleep(100);
        }
    }
}
