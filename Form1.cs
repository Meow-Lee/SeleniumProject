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

namespace N게시_카피
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
        void Find_Fid()
        {
            string[] account = File.ReadAllLines(Application.StartupPath + "\\NDBapi.txt", Encoding.UTF8);
            ACCESS_TOKEN = account[0];
        }
        void Get_Point()
        {
            BestCaptchaSolverAPI bcs = new BestCaptchaSolverAPI(ACCESS_TOKEN);

            string balance = bcs.account_balance();
            balance = balance.Replace("$", "");
            balance = balance.Replace(".", "");

            int bal = int.Parse(balance);
            bal = (bal / 8);
            balance = bal.ToString();

            포인트출력.Text = "Point : " + balance;
        }
        void Check_Point()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://45.32.31.200/api/?name=" + Fid);
                request.Method = "GET";

                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string point_Check = reader.ReadToEnd();
                        point_Check = point_Check.Split(new string[] { "-----" }, StringSplitOptions.None)[2];
                        ndbpoint.Text = point_Check;
                    }
                }
            }
            catch { }
        }
        private void 작업데이터_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int i = 0; i < 작업데이터.Rows.Count; i++)
            {
                // 행 증가 시켜줌
                작업데이터.Rows[i].Cells[0].Value = (i + 1);
                if (Convert.ToString(작업데이터.Rows[i].Cells[6].Value) == "")
                {
                    작업데이터.Rows[i].Cells[6].Value = "대기";
                }
            }
        }
        private void 작업데이터_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e) // RowsAdded랑 코드 같지만 수정할 필요는 없음
        {
            for (int i = 0; i< 작업데이터.Rows.Count; i++)
            {
                작업데이터.Rows[i].Cells[0].Value = (i + 1);
                if (Convert.ToString(작업데이터.Rows[i].Cells[6].Value) == "")
                {
                    작업데이터.Rows[i].Cells[6].Value = "대기";
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
                if (File.Exists(Application.StartupPath + "\\작업백업.txt")) 
                {
                    File.Delete(Application.StartupPath + "\\작업백업.txt");
                }
                for (int i = 0; i < 작업데이터.Rows.Count - 1; i++)
                {
                    FileStream fs = new FileStream(Application.StartupPath + "\\작업백업.txt", FileMode.Append, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                    sw.WriteLine(Convert.ToBoolean(작업데이터.Rows[i].Cells[0].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[1].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[2].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[3].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[4].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[5].Value));
                    sw.Close();
                    fs.Close();
                }
            }
            catch { }

            // 초기 work_state 설정값은 false
            // 만약 게시시작을 했을 때 작동중이라면 진행중이던 스레드 계속 진행
            // work_state가 false, 즉 작동중이지 않다면 새로운 스레드 생성해서 진행
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
            for (int j = 0; j < 작업데이터.Rows.Count - 1; j++)
            {
                // 쓰지 않은 아디 0
                id_donot = 0;

                // 크롬창 두개 생성
                Create_Chrome();
                Create_Chrome2();
                
                // 만약 네이버 로그인 결과가 잘 되어서..true 일때
                if (Naver_Login(Convert.ToString(작업데이터.Rows[j].Cells[1].Value), Convert.ToString(작업데이터.Rows[j].Cells[2].Value), Convert.ToString(작업데이터.Rows[j].Cells[6].Value), Convert.ToString(작업데이터.Rows[j].Cells[8].Value)))
                {
                    // 게시글 생성?
                    Article_Made(Convert.ToString(작업데이터.Rows[j].Cells[5].Value) + ".txt", Convert.ToString(작업데이터.Rows[j].Cells[7].Value) + ".txt");
                    
                    try
                    {
                        // 아이디 성공횟수 증가시키면서 txt파일에 써놓기
                        id_Success++;

                        FileStream fs1 = new FileStream(Application.StartupPath + "\\정상id.txt", FileMode.Append, FileAccess.Write);
                        StreamWriter sw1 = new StreamWriter(fs1, Encoding.UTF8);
                        string list_Name = Convert.ToString(작업데이터.Rows[j].Cells[1].Value);
                        sw1.WriteLine(id_Success + "/" + 작업데이터.Rows.Count.ToString() + "\t" + Convert.ToString(작업데이터.Rows[j].Cells[1].Value) + "\t" + Convert.ToString(작업데이터.Rows[j].Cells[2].Value));
                        sw1.Close();
                        fs1.Close();
                    }
                    catch { }

                    // 로그인이 됬으면
                    if (id_donot == 0)
                    {
                        // 카페 가입 작업 시작
                        // 가입이 성공적으로 되었으면
                        if (Cafe_Join(Convert.ToString(작업데이터.Rows[j].Cells[3].Value) + ".txt", Convert.ToString(작업데이터.Rows[j].Cells[4].Value), Convert.ToString(작업데이터.Rows[j].Cells[5].Value), Convert.ToString(작업데이터.Rows[j].Cells[1].Value), Convert.ToString(작업데이터.Rows[j].Cells[2].Value)))
                        {
                            // 결과창에 O 표시
                            작업데이터.Rows[j].Cells[6].Value = "O";
                        }
                        // 실패 했으면
                        else
                        {
                            // 결과창에 X 표시
                            작업데이터.Rows[j].Cells[6].Value = "X";
                        }
                    }
                    // 만약 로그인 안된 아이디가 있으면
                    else if (id_donot == 1)
                    {
                        // 결과창에 
                        작업데이터.Rows[j].Cells[6].Value = "활X";
                    }

                    // 만약 메세지 띄우기 설정 되어있으면
                    if (IP0.Checked)
                    {
                        MessageBox.Show("아이피 변환 후 확인눌러주세요, 다음아이디 진행합니다");
                    }

                    // 데이터 ON/OFF 설정 되어있으면
                    if (IP1.Checked)
                    {
                        // ip 바꿔줌
                        IP_Change();
                    }
                    // 데이터 ON/OFF_EXPRESS 설정 되어있으면
                    else if (IP2.Checked)
                    {
                        // ip 바꿔줌
                        IP_Change2();
                    }
                }
                // 네이버 로그인이 안되서 Naver_Login 메소드 반환값이 false 일때
                else
                {
                    // 결과 창에 로그인X 결과 표시
                    작업데이터.Rows[j].Cells[6].Value = "로X";

                    // 보안문자 걸려서 ip를 바꿔줘야 한다면 = ip_C가 1이 되는 경우
                    if(ip_C == 1)
                    {
                        // 데이터 ON/OFF 설정 -> IP 바꿔줌
                        if (IP1.Checked)
                        {
                            IP_Change();
                        }
                        // 데이터 ON/OFF_EXPRESS 설정 -> IP 바꿔줌
                        else if (IP2.Checked)
                        {
                            IP_Change2();
                        }
                        // ip 변경 후 다시 ip_C 0으로 변경
                        ip_C = 0;
                    }
                }

                // 크롬창 종료
                Quit_Chrome();
                Quit_Chrome2();

                // 작업한 것 다시 백업
                FileStream fs = new FileStream(Application.StartupPath + "\\작업백업2.txt", FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                sw.WriteLine(Convert.ToString(작업데이터.Rows[j].Cells[0].Value));
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
        bool Naver_Login(string id, string pw, string i1, string i2)
        {
            bool result = false;
            // 라인 우회로그인 체크시
            if (ToLine.Checked)
            {
                try
                {
                    // url 이동
                    driver.Navigate().GoToUrl("https://m.naver.com");
                    driver.Navigate().GoToUrl("https://access.line.me/oauth2/v2.1/login?returnUri=%2Foauth2%2Fv2.1%2Fauthorize%2Fconsent%3Fscope%3Dprofile%2Bfriends%2Bmessage.write%2Btimeline.post%2Bphone%2Bemail%2Bopenid%26response_type%3Dcode%26state%3D5022635175%26redirect_uri%3Dhttps%253A%252F%252Fnid.naver.com%252Foauth%252Fglobal%252FlineCallback.nhn%26client_id%3D1426360231%26showPermissionApproval%3DE%252CH&loginChannelId=1426360231&loginState=kLw7qn5YYGvYYsbwdjD2Bc");
                    
                    // id pw 입력 후 엔터
                    driver.FindElement(By.CssSelector("[id='id']")).SendKeys(id + "@disbox.net");
                    driver.FindElement(By.CssSelector("[pw='passwd']")).SendKeys(pw);
                    driver.FindElement(By.CssSelector("[id='passwd']")).SendKeys(OpenQA.Selenium.Keys.Enter);
                    Thread.Sleep(4000);

                    try
                    {
                        driver.FindElement(By.CssSelector("[name='allow']")).Click();
                        Thread.Sleep(5000);
                    }
                    catch { }

                    // 로그인 후 naver 이동
                    driver.Navigate().GoToUrl("https://m.naver.com");
                    // 로그아웃이 가능하면 로그인 완료
                    if (driver.FindElements(By.CssSelector("[href='javascript:naver.main.logout()']")).Count > 0)
                    {
                        result = true;
                    }
                }
                catch { }
            }
            // 라인 우회로그인 안할시
            else
            {
                // 로그인 시도
                try
                {
                    // 클립보드에 id 복사
                    Clipboard.SetText(id);
                    // url이동
                    driver.Navigate().GoToUrl("https://nid.naver.com/nidlogin.login?svctype=262144&url=http://m.naver.com/aside/");
                    Thread.Sleep(1000);

                    // 3~7 범위 내 난수 생성
                    int Click_Count = rnd.Next(3, 7);

                    // id를 ctrl+v 로 입력
                    driver.FindElement(By.CssSelector("[id='id']")).SendKeys(OpenQA.Selenium.Keys.Control + "v");
                    Thread.Sleep(1000);

                    try
                    {
                        // 패스워드 입력으로 스크롤해서 이동
                        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                        js.ExecuteScript("arguments[0].scrollIntoView();", driver.FindElement(By.CssSelector("[id='pw']")));
                        Thread.Sleep(519);
                    }
                    catch { }

                    // pw 입력 칸 클릭
                    driver.FindElement(By.CssSelector("[id='pw']")).Click();
                    Clipboard.SetText(pw);

                    // pw 입력칸에 ctrl+v로 입력
                    driver.FindElement(By.CssSelector("[id='pw']")).SendKeys(OpenQA.Selenium.Keys.Control + "v");

                    // 엔터
                    driver.FindElement(By.CssSelector("[id='pw']")).SendKeys(OpenQA.Selenium.Keys.Enter);
                    Thread.Sleep(3000);

                    // 만약 보안 문자가 뜬다면
                    if (driver.FindElements(By.CssSelector("[id='captchaimg']")).Count > 0)
                    {
                        // ip_C는 ip가 바뀐상태면 0, 바뀌지 않아 보안문자가 뜨면 1로 설정
                        // result 의 기본 설정 값은 false, 즉 로그인이 안됨
                        ip_C = 1;
                        return result;
                    }

                    // 로그인_재시도1
                    try
                    {
                        driver.FindElements(By.CssSelector("[class='btn']"))[1].Click();
                        Thread.Sleep(5000);
                    }
                    catch { }
                    // 로그인_재시도2
                    try
                    {
                        driver.FindElements(By.CssSelector("[class='btn']"))[1].Click();
                        Thread.Sleep(5000);
                    }
                    catch { }

                    Thread.Sleep(3000);

                    // 정보창 이동
                    driver.Navigate().GoToUrl("https://m.naver.com/aside/");
                    Thread.Sleep(3000);

                    // 정보창 맨 아래 로그인/로그아웃 버튼
                    try
                    {
                        if (driver.FindElements(By.CssSelector("[class*='af_link_info']")).Count > 0)
                        {
                            // 변수에 로그인인지 로그아웃인지 알아냄
                            string VC = driver.FindElement(By.CssSelector("[class*='af_link_info']")).GetAttribute("innerHTML");
                            // VC = VC.Split(new string[] { "<a>", "</a>" }, StringSplitOptions.None)[1];

                            // 로그아웃이면
                            if (VC == "로그아웃")
                            {
                                // 로그인 성공
                                result = true;
                            }
                        }
                    }
                    catch { }
                }
                catch { }
            }
            return result;
        }
        void Create_Chrome()
        {
            try
            {
                ChromeDriverService cds = ChromeDriverService.CreateDefaultService();
                cds.HideCommandPromptWindow = true;

                ChromeOptions Option = new ChromeOptions();
                Option.AddArgument("--window-position=0,0"); // 위치
                Option.AddArgument("--window-size=500,850"); // 기본 창 크기
                Option.AddArgument("disable-infobars"); // 정보창 안띄움

                // 크롬창 생성
                driver = new ChromeDriver(cds, Option);
            }
            catch { }
        }
        void Create_Chrome2()
        {
            try
            {
                ChromeDriverService cds = ChromeDriverService.CreateDefaultService();
                cds.HideCommandPromptWindow = true;

                ChromeOptions Option = new ChromeOptions();
                Option.AddArgument("--window-position=501,0"); // 위치
                Option.AddArgument("--window-size=500,850"); // 기본 창 크기
                Option.AddArgument("disable-infobars"); // 정보창 안띄움

                // 크롬창 생성
                driver2 = new ChromeDriver(cds, Option);

            }
            catch { }
        }
        void Quit_Chrome()
        {
            try
            {
                // 크롬창 종료
                driver.Quit();
            }
            catch { }
        }
        void Quit_Chrome2()
        {
            try
            {
                // 크롬창 종료
                driver2.Quit();
            }
            catch { }
        }
        void Article_Made(String article, string link)
        {
            // 내용파일
            string[] article_Site = File.ReadAllLines(article, Encoding.UTF8);
            // 제휴링크
            string[] article_Link = File.ReadAllLines(link, Encoding.UTF8);
            string site_Go = article_Site[0];

            // url로 이동
            try
            {
                // 2번 크롬창 url 이동
                driver2.Navigate().GoToUrl(site_Go);
                Thread.Sleep(3900);
            }
            catch { }

            IJavaScriptExecutor js = (IJavaScriptExecutor)driver2;

            for (int i = 0; i < 5; i++)
            {
                // 제대로 수행되면 break
                try
                {  
                    // 입력하려는 메소드 -> 대충 기사제목 css인듯
                    js.ExecuteScript("arguments[0].innerText = ''", driver2.FindElement(By.CssSelector("[class='tl_article_header']")));
                    Thread.Sleep(300);
                    break;
                }
                catch { }
            }

            IWebElement element = driver2.FindElement(By.CssSelector("[target='_blank']"));

            for (int i = 0; i < 10; i++)
            {
                // 제대로 수행되면 break
                try
                {
                    js.ExecuteScript("arguments[0].setAttribute('href', arguments[1]);", element, article_Link[0]);
                    Thread.Sleep(2000);
                    break;
                }
                catch { }
            }
            Thread.Sleep(700);
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
        bool Cafe_Join(string cafe, string subject, string content, string id, string pw)
        {
            // 카페파일, 제목파일, 내용파일, id, pw
            // result는 가입 성공 여부
            bool result = false;
            // 난수생성 변수
            Random rnd1 = new Random();
            // 카페리스트 읽기
            string[] cafe_List = File.ReadAllLines(cafe, Encoding.UTF8);
            // m은 카페리스트의 개수 범위내에서 무작위로 선정
            int m = rnd1.Next(0, (cafe_List.Count() - 1));
            
            // 카페리스트 개수만큼 반복하면서
            for (int i = 0; i < cafe_List.Count(); i++)
            {
                // 보안문자 오류 방지
                bool flag = false;

                // 실, 비실 아디 구분
                // 비실 기준 false
                bool isReal = false;

                // alert창 넘기기
                try
                {
                    IAlert alert = driver.SwitchTo().Alert();
                    alert.Accept();
                    Thread.Sleep(1500);
                }
                catch { }

                // alert창 넘기기
                try
                {
                    IAlert alert = driver.SwitchTo().Alert();
                    alert.Accept();
                    Thread.Sleep(1500);
                }
                catch { }

                // 처음에만
                if (i == 0)
                {
                    // 실, 비실 구분
                    try
                    {
                        driver.Navigate().GoToUrl("https://nid.naver.com/user2/help/myInfoV2?lang=ko_KR");
                        // 실명이면 true
                        if (driver.FindElements(By.CssSelector("[onclick='changeName()']")).Count > 0)
                        {
                            isReal = true;
                        }
                    }
                    catch { }
                }

                try
                {
                    // 카페 주소를 담을 string 변수
                    string cafe1 = "";
                    string cafe2 = "";
                    string cafe3 = "";

                    // 만약 카페순서를 random하는 설정이 체크 되었을 경우
                    if (checkBox1.Checked)
                    {
                        // 카페리스트 범위내에서 선정한 무작위수 + i값 이 카페리스트 개수보다 크면
                        if (m + i >= cafe_List.Count())
                        {
                            // 카페리스트의 m+i-cafe_list.Count 번부터 시작
                            // split으로 뒤의 주소를 나누고 m.cafe.naver.com에 붙임
                            // url 이동
                            cafe1 = cafe_List[m + i - cafe_List.Count()].Split(new string[] { "/" }, StringSplitOptions.None)[3];
                            cafe1 = "https://m.cafe.naver.com/" + cafe1;
                            driver.Navigate().GoToUrl(cafe1);
                        }
                        // 작은 경우
                        else
                        {
                            // 그렇지 않으면 m+i번 카페부터 뒷부분 split하여 주소 붙인 다음
                            // url 이동
                            cafe2 = cafe_List[m + i].Split(new string[] { "/" }, StringSplitOptions.None)[3];
                            cafe2 = "https://m.cafe.naver.com/" + cafe2;
                            driver.Navigate().GoToUrl(cafe2);
                        }
                    }
                    // 카페순서를 random 하지 않을 경우
                    else
                    {
                        // random하지 않으면 처음부터 시작
                        // 뒷부분 split하여 앞 주소랑 붙이고
                        // url 이동
                        cafe3 = cafe_List[i].Split(new string[] { "/" }, StringSplitOptions.None)[3];
                        cafe3 = "https://m.cafe.naver.com/" + cafe3;
                        driver.Navigate().GoToUrl(cafe3);
                    }

                    Thread.Sleep(1193);

                    // alert창 넘기기
                    try
                    {
                        IAlert alert = driver.SwitchTo().Alert();
                        alert.Accept();
                        Thread.Sleep(1500);
                    }
                    catch { }

                    // alert창 넘기기
                    try
                    {
                        IAlert alert = driver.SwitchTo().Alert();
                        alert.Accept();
                        Thread.Sleep(1500);
                    }
                    catch { }

                    // 처음 방문했을때 뜨는 네이버 공지? 창 닫기 버튼누름
                    try
                    {
                        Thread.Sleep(700);
                        driver.FindElement(By.CssSelector("[class='ButtonBase ButtonBase--gray']")).Click();
                        Thread.Sleep(1711);
                    }
                    catch { }

                    // 만약 가입창이면
                    if (driver.FindElements(By.CssSelector("[class='btn_join']")).Count > 0)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            // 제대로 수행시 break
                            try
                            {
                                // 가입하기 클릭
                                driver.FindElement(By.CssSelector("[class='btn_join']")).Click();
                                Thread.Sleep(3000);
                                break;
                            }
                            catch
                            {
                                Thread.Sleep(1000);
                            }                            
                        }

                        try
                        {
                            // 여기에 텀을 주셈
                            Thread.Sleep(Convert.ToInt32(Term00.Text) * 1000);
                        }
                        catch { { } }

                        try
                        {
                            string isReal_Text_alert = driver.SwitchTo().Alert().Text;

                            try
                            {
                                isReal_Text_alert = isReal_Text_alert.Split(new string[] { " " }, StringSplitOptions.None)[0];

                                if (isReal_Text_alert == "실명")
                                {
                                    driver.SwitchTo().Alert().Accept();
                                    Sleep_time10();
                                    continue;
                                }
                            }
                            catch { }
                        }
                        catch { }

                        try
                        {
                            // 메시지 창의 텍스트 기록
                            string alert_Text33 = driver.SwitchTo().Alert().Text;

                            try
                            {
                                // 공백전 문자만 받고
                                alert_Text33 = alert_Text33.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                // 받은 문자열이 '아이디가' 이면
                                if (alert_Text33 == "아이디가")
                                {
                                    // alert 넘기고 Thread.sleep
                                    // 가입여부는 false
                                    driver.SwitchTo().Alert().Accept();
                                    Sleep_time10();
                                    result = false;
                                    return result;
                                }
                            }
                            catch { }
                        }
                        catch { }

                        if (driver.FindElements(By.CssSelector("[class='lang_select']")).Count == 0)
                        {
                            // 활동정지 이면
                            if (driver.FindElements(By.CssSelector("[class='icn warning']")).Count > 0)
                            {
                                string icn_Ment = "";
                                try
                                {
                                    // 변수에 split으로 text를 담고
                                    icn_Ment = driver.FindElement(By.CssSelector("h2")).GetAttribute("innerHTML");
                                    icn_Ment = icn_Ment.Split(new string[] { "<br>" }, StringSplitOptions.None)[1];
                                    icn_Ment = icn_Ment.Split(new string[] { " " }, StringSplitOptions.None)[1];
                                }
                                catch { }

                                // 만약 이용정지 감지 설정 되어있으면
                                if (user_Test1.Checked)
                                {
                                    // 경고창에서 담은 변수 텍스트가 '카페' 이면 넘어가고
                                    if (icn_Ment == "카페")
                                    { }
                                    // '아이디가'로 시작되면
                                    else if (icn_Ment == "아이디가")
                                    {
                                        try
                                        {
                                            // 실패한 아이디 개수 늘려주고
                                            id_Failed++;
                                            
                                            // 이용정지 id.txt 에 기록
                                            FileStream fs1 = new FileStream(Application.StartupPath + "\\정이용정지id.txt", FileMode.Append, FileAccess.Write);
                                            StreamWriter sw1 = new StreamWriter(fs1, Encoding.UTF8);
                                            sw1.WriteLine(id_Failed + "/" + 작업데이터.Rows.Count.ToString() + "\t" + id + "\t" + pw);
                                            sw1.Close();
                                            fs1.Close();
                                        }
                                        catch { }

                                        // 가입여부는 실패로 설정
                                        result = false;

                                        return result;
                                    }
                                }
                            }
                        }

                        //  아디잠김 테스트
                        if (driver.FindElements(By.CssSelector("[class='lang_select']")).Count > 0)
                        {
                            if (Naver_Login(id, pw, "1", "2"))
                            { }
                            else
                            {
                                result = false;
                                return result;
                            }
                        }


                        // 잠김감지 설정 체크
                        if (idcheck1.Checked)
                        {
                            // 로그인 창이 뜨고
                            if (driver.FindElements(By.CssSelector("[class='lang_select']")).Count > 0)
                            {
                                // 잠겼는지 체크 위해서 로그인
                                if (Naver_Login(id, pw, "1", "2"))
                                { }
                                // 잠겼으면 나감
                                else
                                {
                                    //MessageBox.Show("잠김");
                                    result = false;
                                    return result;
                                }
                            }
                        }

                        string rnd_Nick = "";
                        // 고정닉네임 설정 체크
                        if (FixedIdCheck.Checked)
                        {
                            rnd_Nick = Fixed_Nick();
                        }
                        else
                        {
                            rnd_Nick = Random_Nick();
                        }

                        // 닉네임 자동 설정 되어있으면
                        if (NName.Checked || FixedIdCheck.Checked)
                        {
                            // 1,2,3번 카페만 닉네임 중복 안되게 다르게 설정하고 그 뒤 카페들은 쭉 동일한 닉네임으로 설정
                            try
                            {
                                // 별명 칸 지우고 랜덤 닉으로 함
                                Thread.Sleep(2231);
                                driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                driver.FindElement(By.CssSelector("[placeholder='별명']")).Clear();
                                driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(rnd_Nick);
                                Thread.Sleep(2371);
                                /* if (i == 0)
                                 {
                                     // 별명 칸 지우고 랜덤 닉으로 함
                                     Thread.Sleep(2231);
                                     driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                     driver.FindElement(By.CssSelector("[placeholder='별명']")).Clear();
                                     driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(rnd_Nick);
                                     Thread.Sleep(2371);
                                 }
                                 else if (i == 1)
                                 {
                                     // 별명 칸 지우고 랜덤 닉으로 함
                                     Thread.Sleep(2231);
                                     driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                     driver.FindElement(By.CssSelector("[placeholder='별명']")).Clear();
                                     driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(rnd_Nick);
                                     Thread.Sleep(2371);
                                 }
                                 else if (i == 2)
                                 {
                                     // 별명 칸 지우고 랜덤 닉으로 함
                                     Thread.Sleep(2231);
                                     driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                     driver.FindElement(By.CssSelector("[placeholder='별명']")).Clear();
                                     driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(rnd_Nick);
                                     Thread.Sleep(2371);
                                 }*/
                            }
                            catch { }
                        }

                        // 이름 조합
                        string ans1 = "";
                        string ans2 = "";
                        string ans3 = "";
                        string ans4 = "";
                        string ans5 = "";
                        string ans6 = "";

                        Random random = new Random();
                        int limit = 3;

                        string input1 = "abcdefghijklmnopqrstuvwxyz0123456789";

                        // 남성 이름
                        string[] maleNames = new string[] { "대언", "대연", "대영", "대원", "대윤", "대은", "대율", "대인", "대한", "대현", "대형", "대환", "대훈", "대경", "대권", "대규", "대융", "대우", "대후", "다헌", "다형", "다환", "다훈", "도연", "도영", "도원", "도윤", "도율", "도헌", "도현", "도훈", "동언", "동연", "동영", "동예", "동완", "동원", "동운", "동윤", "동은", "동율", "동인", "동한", "동해", "동헌", "동현", "동혜", "동환", "동훈", "동희", "동율", "동후", "동우", "두영", "두윤", "두율", "두환", "두훈", "래원", "래헌", "래환", "래훈", "태연", "태영", "태원", "태윤", "태은", "태율", "태한", "태헌", "태현", "태환", "태훈", "태율", "류원", "윤관", "윤교", "윤규", "윤기", "윤겸", "윤렬", "윤태", "은교", "은겸", "은규", "은률", "은린", "한영", "한율", "한오", "한준", "한성", "연우", "연욱", "영인", "우원", "원영", "원오", "원우", "원일", "윤열", "윤오", "윤용", "윤우", "윤일", "은우", "은오", "은율", "하원", "하율", "하윤", "하일", "하은", "현호", "현우", "현일", "원훈", "원형", "원호", "원혁", "유한", "유현", "유형", "유환", "윤하", "윤한", "윤현", "윤헌", "윤형", "윤호", "윤혁", "윤해", "윤후", "이헌", "이한", "효헌", "연서", "우성", "유상", "유성", "유신", "윤상", "윤성", "윤서", "윤세", "윤섭", "윤수", "은서", "은상", "은세", "은성", "은수", "은섭", "하성", "한상", "한수", "효상", "효성", "희상", "희성", "희수", "아준", "여준", "연재", "영준", "우재", "우준", "우진", "원재", "원정", "원준", "유준", "유진", "윤재", "윤제", "윤준", "윤찬", "유찬", "윤진", "은재", "은준", "은찬", "은진", "이준", "이찬", "하준", "하진", "효준", "효찬", "효재", "영재", "영찬", "영준", "원찬", "유창", "윤채", "윤철", "은찬", "은채", "의찬", "현서", "은후", "연후", "윤후", "현후", "영후", "현준", "예준", "예찬", "영준", "영찬", "우찬", "원준-이안", "이완", "이헌", "이훈", "이준", "이찬", "이수", "이호", "이황", "상아", "상연", "상영", "상완", "상우", "상원", "상윤", "", "상헌", "상훈", "상현", "상호", "상환", "상후", "상희", "상율", "상일", "상엽", "서훈", "서환", "서후", "서준", "서진", "서빈", "성연", "성영", "성완", "성우", "성원", "성윤", "성헌", "성현", "성훈", "성호", "성후", "성희", "성율", "성하", "성한", "성은", "성일", "성혁", "성엽", "성수", "성재", "성진", "성준", "성찬", "성민", "성빈", "세연", "세영", "세은", "세완", "세원", "세윤", "세현", "세훈", "세호", "세후", "세율", "세희", "세한", "세일", "세혁", "세준", "세진", "세민", "세빈", "세명", "송훈", "송후", "송혁", "송민", "송빈", "수훈", "수헌", "수한", "수혁", "수성", "수민", "수빈", "승연", "승현", "승완", "승우", "승원", "승윤", "승헌", "승훈", "승한", "승호", "승환", "승후", "승일", "승엽", "승혁", "승진", "승준", "승민", "승빈", "시현", "시훈", "시윤", "시환", "시율", "시우", "시원", "시후", "시혁", "시헌", "시진", "시준", "시민", "시빈", "장연", "장완", "장원", "장우", "장윤", "장헌", "장훈", "장우", "장현", "장호", "장혁", "재연", "재영", "재우", "재원", "재헌", "재훈", "재현", "재호", "재환", "재율", "재일", "재혁", "재준", "재진", "재찬", "재성", "재민", "재빈", "조현", "조영", "조원", "종연", "종완", "종우", "종원", "종윤", "종헌", "종훈", "종현", "종호", "종환", "종후", "종한", "종일", "종혁", "종수", "종성", "종찬", "종민", "종재", "종빈", "종명", "주원", "주호", "주환", "주한", "주헌", "주훈", "주혁", "주찬", "주성", "주빈", "준영", "준아", "준연", "준완", "준우", "준원", "준현", "준헌", "준호", "준후", "준일", "준혁", "준상", "준성", "지운", "지완", "지한", "지헌", "주훈", "지율", "지환", "지호", "지후", "지혁", "지민", "지빈", "지명", "진호", "진영", "진혁", "차헌", "차훈", "차민", "차빈", "찬영", "찬연", "찬우", "찬원", "찬헌", "찬현", "찬호", "찬후", "찬율", "찬혁", "명진", "명성", "명세", "명민", "명재", "명제", "명준", "명찬", "민서", "민준", "민찬", "민건", "민겸", "민국", "민관", "민규", "민기", "민상", "민세", "민성", "민서", "민준", "민찬", "민건", "민겸", "민국", "민관", "민규", "민기", "민상", "민세", "민성", "범성", "범찬", "범교", "범준", "범규", "범기", "범상", "범세", "범창" };
                        string FirstName = "";

                        // 무작위로 이름 선정
                        Random rand = new Random(DateTime.Now.Second);
                        FirstName = maleNames[rand.Next(0, maleNames.Length - 1)];

                        // 성
                        Random random2 = new Random();
                        string LastName = "김김김김김김김김김이이이이이이박박박최최최정정강조윤장임한오서진권황안송전홍유고문양손배조백허유남심노하곽성차주우구신전민진엄지채원천방공강현함변신표탁";

                        // 범위 내에서 성을 고름
                        var chars41 = Enumerable.Range(1, 1).Select(x => LastName[random2.Next(0, LastName.Length)]);

                        // 이름 합치기
                        string name31 = new string(chars41.ToArray());
                        string ans60 = name31 + FirstName;

                        // limit 변수 난수 생성
                        limit = random.Next(2, 6);
                        var chars1 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        limit = random.Next(2, 6);
                        var chars2 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        limit = random.Next(2, 6);
                        var chars3 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        limit = random.Next(2, 6);
                        var chars4 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        limit = random.Next(2, 6);
                        var chars5 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        limit = random.Next(2, 6);
                        var chars6 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);

                        ans1 = new string(chars1.ToArray());
                        ans2 = new string(chars2.ToArray());
                        ans3 = new string(chars3.ToArray());
                        ans4 = new string(chars4.ToArray());
                        ans5 = new string(chars5.ToArray());
                        ans6 = new string(chars6.ToArray());

                        try
                        {
                            // 이미 사용중인 별명이면
                            if (driver.FindElements(By.CssSelector("[class='input_message alert']")).Count > 0)
                            {
                                for (int j = 0; j < 5; j++)
                                {
                                    // 글자하나를 다시 고르고 닉네임 칸 누른다음
                                    //string new_ans6 = name_API1();
                                    int new_num = rnd.Next(1, 99);
                                    string new_ans = new_num.ToString();
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();

                                    // 맨 오른쪽으로 가서
                                    for (int k = 0; k < 8; k++)
                                    {
                                        driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(OpenQA.Selenium.Keys.ArrowRight);
                                        Sleep_time100();
                                    }
                                    Sleep_time10();

                                    // 한글자 지워주고
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(OpenQA.Selenium.Keys.Backspace);
                                    Sleep_time10();

                                    // 위에서 다시 고른 글자를 넣어줌
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(new_ans);
                                    Sleep_time11();

                                    // 중복별명이라고 뜨지 않으면 break
                                    if (driver.FindElements(By.CssSelector("[class='input_message alert']")).Count == 0)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }

                        // 가입질문 1~5번 클릭
                        try
                        {
                            driver.FindElement(By.CssSelector("[for='radio_join_question_1_0']")).Click();
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[for='radio_join_question_2_0']")).Click();
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[for='radio_join_question_3_0']")).Click();
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[for='radio_join_question_4_0']")).Click();
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[for='radio_join_question_5_0']")).Click();
                            Sleep_time10();
                        }
                        catch { }

                        // 가입 질문 1~9번 까지 클릭해서 답을 넣음
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_1']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_1']")).SendKeys(ans1);
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_2']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_2']")).SendKeys(ans2);
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_3']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_3']")).SendKeys(ans3);
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_4']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_4']")).SendKeys(ans4);
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_5']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_5']")).SendKeys(ans5);
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_6']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_6']")).SendKeys(ans6);
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_7']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_7']")).SendKeys(ans6 + "e");
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_8']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_8']")).SendKeys("f" + ans5 + "w");
                            Sleep_time10();
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_9']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_9']")).SendKeys("a" + ans5);
                            Sleep_time10();
                        }
                        catch { }

                        // 만약에 보안문자가 뜨면
                        if (driver.FindElements(By.CssSelector("[class='chaptcha_img']")).Count > 0)
                        {
                            for (int z = 0; z < 5; z++)
                            {
                                try
                                {
                                    // 중복방지
                                    // 파일이 이미 있으면 삭제
                                    if (File.Exists(Application.StartupPath + "\\화면캡쳐.png"))
                                    {
                                        File.Delete(Application.StartupPath + "\\화면캡쳐.png");
                                    }
                                    if (File.Exists(Application.StartupPath + "\\보안코드.png"))
                                    {
                                        File.Delete(Application.StartupPath + "\\보안코드.png");
                                    }

                                    // 보안 문자로 마우스 움직여서 보고
                                    try
                                    {
                                        for (int x = 0; x < 5; x++)
                                        {
                                            new Actions(driver).MoveToElement(driver.FindElement(By.CssSelector("[class='chaptcha_img']"))).Perform();
                                            Thread.Sleep(100);
                                        }
                                    }
                                    catch { }

                                    // 보안 문자 입력 창 누름
                                    try
                                    {
                                        if (z > 2)
                                        {
                                            driver.FindElement(By.CssSelector("[id='label_join_captcha']")).Click();
                                        }
                                    }
                                    catch { }

                                    // Yvalue 만큼 스크롤 이동
                                    try
                                    {
                                        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                                        Thread.Sleep(519);

                                        int Yvalue = rnd.Next(600, 813);
                                        string Yvalue1 = Yvalue.ToString();
                                        js.ExecuteScript("window.scrollBy(0," + "+" + +Yvalue + ")");
                                    }
                                    catch { }

                                    // 보안문자 스크린샷 찍고 저장
                                    try
                                    {
                                        TakeScreenshot(driver.FindElement(By.CssSelector("[class='chaptcha_img']")), Application.StartupPath + "\\보안코드.png");
                                    }
                                    catch (Exception ex) {
                                        FileStream fs1 = new FileStream(Application.StartupPath + "\\Error.txt", FileMode.Append, FileAccess.Write);
                                        StreamWriter sw1 = new StreamWriter(fs1, Encoding.UTF8);

                                        sw1.WriteLine(DateTime.Now.ToString("yyyy-MM-dd ") + ex.ToString());
                                        sw1.Close();
                                        fs1.Close();

                                        flag = true;
                                        break;
                                    }

                                    // 보안 문자 답을 넣고 클릭
                                    try
                                    {
                                        string captcha_Answer = captcha_image1();
                                        driver.FindElement(By.CssSelector("[id='label_join_captcha']")).Click();
                                        driver.FindElement(By.CssSelector("[id='label_join_captcha']")).Clear();
                                        driver.FindElement(By.CssSelector("[id='label_join_captcha']")).SendKeys(captcha_Answer);
                                        driver.FindElement(By.CssSelector("[class='join_btn_box']")).Click();
                                        Thread.Sleep(1931);

                                        string alert_Text3 = "";
                                        try
                                        {
                                            // 메세지 텍스트 담고
                                            alert_Text3 = driver.SwitchTo().Alert().Text;
                                            try
                                            {
                                                // 메시지 텍스트 담고 split해서 공백 전 string 남김
                                                alert_Text3 = driver.SwitchTo().Alert().Text;
                                                try
                                                {
                                                    alert_Text3 = alert_Text3.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                                }
                                                catch { }

                                                // 남긴 string이 '별명은' 이면
                                                if (alert_Text3 == "별명은")
                                                {
                                                    // 메세지 넘기고(확인하고)
                                                    driver.SwitchTo().Alert().Accept();
                                                    Thread.Sleep(2231);

                                                    for (int j = 0; j < 5; j++)
                                                    {
                                                        // 글자하나 다시 가져오고 닉네임 칸 클릭
                                                        string ans06 = name_API1();
                                                        driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();

                                                        // 쓰여있는 닉네임 맨 오른쪽으로가서
                                                        for (int k = 0; k < j; j++)
                                                        {
                                                            driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(OpenQA.Selenium.Keys.ArrowRight);
                                                            Sleep_time100();
                                                        }
                                                        Sleep_time10();

                                                        // 한글자 지우고
                                                        driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(OpenQA.Selenium.Keys.Backspace);
                                                        Sleep_time10();

                                                        // 새로 가져온 글자를 넣음
                                                        driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(ans06);
                                                        Sleep_time11();

                                                        // 사용중인 별명이 아니면 break
                                                        if (driver.FindElements(By.CssSelector("[class='input_message alert']")).Count == 0)
                                                        {
                                                            break;
                                                        }
                                                    }
                                                    // 가입버튼 클릭
                                                    driver.FindElement(By.CssSelector("[class='join_btn_box']")).Click();
                                                    Thread.Sleep(1371);
                                                }
                                            }
                                            catch { }
                                        }
                                        catch { }

                                        try
                                        {
                                            // 메시지 텍스트 받고
                                            alert_Text3 = driver.SwitchTo().Alert().Text;
                                            try
                                            {
                                                // split 한 다음
                                                alert_Text3 = alert_Text3.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                            }
                                            catch { }

                                            // 그 텍스트가 '이미' 이면
                                            if (alert_Text3 == "이미")
                                            {
                                                // 메시지 넘어가고
                                                driver.SwitchTo().Alert().Accept();
                                                Sleep_time10();

                                                // 가입여부 성공
                                                result = true;

                                                break;
                                            }
                                        }
                                        catch { }

                                        // 만약 보안문자가 안뜨면
                                        if (driver.FindElements(By.CssSelector("[class='chaptcha_img']")).Count == 0)
                                        {
                                            // 가입성공
                                            result = true;
                                            break;
                                        }

                                        // 만약 중복 별명이면
                                        if (driver.FindElements(By.CssSelector("[class='input_message alert']")).Count == 1)
                                        {
                                            // split으로 '보' 이전 글자 길이 확인
                                            string error_MSG = driver.FindElement(By.CssSelector("[class='input_message alert']")).GetAttribute("innerHTML");
                                            int count_Answer11 = error_MSG.Split('보').Length - 1;

                                            // 글자가 없으면
                                            if (count_Answer11 == 0)
                                            {
                                                for (int j = 0; j < i; i++)
                                                {
                                                    // 새로 글자하나 받고 맨오른쪽에 붙임
                                                    string ans06 = name_API1();
                                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                                    Sleep_time10();
                                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(OpenQA.Selenium.Keys.Backspace);
                                                    Sleep_time10();
                                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(ans06);
                                                    Sleep_time11();

                                                    // 만약 중복별명이 아니면 break
                                                    if (driver.FindElements(By.CssSelector("[class='input_message alert']")).Count == 0)
                                                    {
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        // 보안문자가 뜨면
                                        if (driver.FindElements(By.CssSelector("[class='chaptcha_img']")).Count == 1)
                                        {
                                            // 보안 문자~~~
                                            driver.FindElement(By.CssSelector("[id='label_join_captcha']")).Clear();
                                            new Actions(driver).MoveToElement(driver.FindElement(By.CssSelector("[class='cafe_name']"))).Perform();
                                            Thread.Sleep(100);

                                            // 만약 가입질문이 있고 2번질문은 없다면
                                            if (driver.FindElements(By.CssSelector("[id='label_join_question_1']")).Count > 0 || driver.FindElements(By.CssSelector("[for='radio_join_question_1_0']")).Count > 0)
                                            {
                                                if (driver.FindElements(By.CssSelector("[for='radio_join_question_2_0']")).Count == 0)
                                                {
                                                    if (driver.FindElements(By.CssSelector("[id='label_join_question_2']")).Count == 0)
                                                    {
                                                        // 스크롤을 Yvalue만큼 내림
                                                        try
                                                        {
                                                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                                                            Thread.Sleep(519);

                                                            int Yvalue = rnd.Next(600, 813);
                                                            string Yvalue1 = Yvalue.ToString();
                                                            js.ExecuteScript("window.scrollBy(0," + "+" + +Yvalue + ")");
                                                        }
                                                        catch { }
                                                    }
                                                }
                                            }
                                            // 만약 가입질문이 없으면
                                            else if (driver.FindElements(By.CssSelector("[id='label_join_question_1']")).Count == 0 || driver.FindElements(By.CssSelector("[for='radio_join_question_1_0']")).Count == 0)
                                            {
                                                // Yvalue만큼 스크롤 내림
                                                try
                                                {
                                                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                                                    Thread.Sleep(519);

                                                    int Yvalue = rnd.Next(600, 813);
                                                    string Yvalue1 = Yvalue.ToString();
                                                    js.ExecuteScript("window.scrollBy(0," + "+" + +Yvalue + ")");
                                                }
                                                catch { }
                                            }
                                        }
                                        // 보안문자 버튼 클릭
                                        driver.FindElement(By.CssSelector("[class='chaptcha_btn']")).Click();
                                        Thread.Sleep(3000);
                                    }
                                    catch { }

                                }
                                catch (Exception ex) {
                                    FileStream fs1 = new FileStream(Application.StartupPath + "\\Error.txt", FileMode.Append, FileAccess.Write);
                                    StreamWriter sw1 = new StreamWriter(fs1, Encoding.UTF8);

                                    sw1.WriteLine(DateTime.Now.ToString("yyyy-MM-dd ") + ex.ToString());
                                    sw1.Close();
                                    fs1.Close();

                                    flag = true;
                                    break;
                                }
                            }
                        }
                        // 보안 문자가 안뜬 경우
                        else
                        {
                            // 가입버튼이있는경우
                            if (driver.FindElements(By.CssSelector("[class='join_btn_box']")).Count > 0)
                            {
                                // 가입성공 
                                result = true;
                                driver.FindElement(By.CssSelector("[class='join_btn_box']")).Click();
                            }
                            // 없는경우엔 암것도 안함
                            else
                            {
                            }

                        }

                        // 보안문자 예외면 다음 카페로 이동하기
                        if (flag)
                        {
                            continue;
                        }

                        // 가입버튼 한번 더 누름
                        try
                        {
                            driver.FindElement(By.CssSelector("[class='join_btn_box']")).Click();
                        }
                        catch { }

                        Thread.Sleep(2231);

                        string alert_Text = "";

                        try
                        {
                            // 메세지 텍스트 받아서 split
                            alert_Text = driver.SwitchTo().Alert().Text;
                            try
                            {
                                alert_Text = driver.SwitchTo().Alert().Text;
                                try
                                {
                                    alert_Text = alert_Text.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                }
                                catch { }

                                // 텍스트 내용이 '카페내' 이면 확인 하고 닉네임 란에 쓰고 가입 클릭
                                if (alert_Text != "카페내")
                                {
                                    driver.SwitchTo().Alert().Accept();
                                    Thread.Sleep(2231);
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(ans6);
                                    Thread.Sleep(2371);
                                    driver.FindElement(By.CssSelector("[class='join_btn_box']")).Click();
                                    Thread.Sleep(1371);

                                    //가입실패
                                }
                            }
                            catch { }
                        }
                        catch { }
                    }
                    // 가입버튼이 아닌 글쓰기 버튼이라면
                    else
                    {
                        // 가입불가.txt 에 기록
                        if (driver.FindElements(By.CssSelector("[class='btn_write']")).Count == 0)
                        {
                            FileStream fs2 = new FileStream(Application.StartupPath + "\\가입불가.txt", FileMode.Append, FileAccess.Write);
                            StreamWriter sw2 = new StreamWriter(fs2, Encoding.UTF8);
                            string Nurl = driver.Url;
                            sw2.WriteLine(Nurl);
                            sw2.Close();
                            fs2.Close();
                            Thread.Sleep(653);
                        }
                    }

                    //게시부분
                    // 가입O게시X 설정 되어있으면
                    if (Join1.Checked)
                    { }
                    // 설정 안되어있으면
                    else
                    {
                        try
                        {
                            // 메세지 텍스트 받고 확인
                            string new_Alert = "";
                            new_Alert = driver.SwitchTo().Alert().Text;
                            try
                            {
                                driver.SwitchTo().Alert().Accept();
                                Sleep_time11();
                                //가입실패
                            }
                            catch { Sleep_time10(); }
                        }
                        catch { }

                        // 카페순서 랜덤 체크 되어있으면
                        if (checkBox1.Checked)
                        {
                            // 만약 카페리스트 개수보다 크면 주소 split 했을 때 뒷부분과 연결해서 url 연결
                            if (m + i >= cafe_List.Count())
                            {
                                cafe1 = cafe_List[m + i - cafe_List.Count()].Split(new string[] { "/" }, StringSplitOptions.None)[3];
                                cafe1 = "https://m.cafe.naver.com/" + cafe1;
                                driver.Navigate().GoToUrl(cafe1);
                            }
                            // 리스트 개수보다 작으면 주소 split 했을 때 뒷부분과 연결해서 url 연결
                            else
                            {
                                cafe2 = cafe_List[m + i].Split(new string[] { "/" }, StringSplitOptions.None)[3];
                                cafe2 = "https://m.cafe.naver.com/" + cafe2;
                                driver.Navigate().GoToUrl(cafe2);
                            }
                        }
                        // 랜덤체크 안되어있으면
                        else
                        {
                            // 뒷부분 연결해서 url 연결
                            cafe3 = cafe_List[i].Split(new string[] { "/" }, StringSplitOptions.None)[3];
                            cafe3 = "https://m.cafe.naver.com/" + cafe3;
                            driver.Navigate().GoToUrl(cafe3);
                        }
                        Thread.Sleep(2500);

                        // 첨 가입했을때 공지의 닫기 누름
                        try
                        {
                            driver.FindElement(By.CssSelector("[class='ButtonBase ButtonBase--gray']")).Click();
                            Thread.Sleep(1711);
                        }
                        catch { }

                        try
                        {
                            driver.FindElement(By.CssSelector("[class*='btn_clse _click']")).Click();
                            Thread.Sleep(2711);
                        }
                        catch { }

                        try
                        {
                            driver.FindElement(By.CssSelector("[class*='btn_lyr_clse _click']")).Click();
                            Thread.Sleep(2711);
                        }
                        catch { }

                        try
                        {
                            driver.FindElement(By.CssSelector("[class*='close _click(Manager']")).Click();
                            Thread.Sleep(2711);
                        }
                        catch { }

                        try
                        {
                            driver.FindElements(By.CssSelector("[class*='close _click(Manager']"))[1].Click();
                            Thread.Sleep(2711);
                        }
                        catch { }

                        try
                        {
                            driver.FindElement(By.CssSelector("[class='ButtonBase ButtonBase--gray']")).Click();
                            Thread.Sleep(1711);
                        }
                        catch { }

                        try
                        {
                            driver.FindElement(By.CssSelector("[class='btn_clse']")).Click();
                            Thread.Sleep(1711);
                        }
                        catch { }

                        // 게시 버튼
                        if (driver.FindElements(By.CssSelector("[class='btn_write']")).Count > 0)
                        {
                            try
                            {
                                driver.FindElement(By.CssSelector("[class*='btn_lyr_clse']")).Click();
                                Thread.Sleep(1239);
                            }
                            catch { }
                            Thread.Sleep(1139);
                            try
                            {
                                driver.FindElement(By.CssSelector("[class*='btn_clse_click']")).Click();
                                Thread.Sleep(3739);
                            }
                            catch { }
                            try
                            {
                                driver.FindElements(By.CssSelector("[class='btn_write']"))[1].Click();
                                Thread.Sleep(2839);
                            }
                            catch { }
                            try
                            {
                                driver.FindElement(By.CssSelector("[class='se-popup-close-button']")).Click();
                                Thread.Sleep(2639);
                            }
                            catch { }
                            try
                            {
                                driver.FindElement(By.CssSelector("[class='se-component-content']")).Click();
                                Thread.Sleep(2000);
                            }
                            catch { }
                            try
                            {
                                driver.FindElement(By.CssSelector("[class='se-popup-close-button']")).Click();
                                Thread.Sleep(1703);
                            }
                            catch { }

                            // 아이디가 이용정지라서 로그인 창이 안뜨는 경우
                            if (driver.FindElements(By.CssSelector("[class='lang_select']")).Count == 0)
                            {
                                // 활동정지 이면
                                if (driver.FindElements(By.CssSelector("[class='icn warning']")).Count > 0)
                                {
                                    string icn_Ment = "";
                                    try
                                    {
                                        // 변수에 split으로 text를 담고
                                        icn_Ment = driver.FindElement(By.CssSelector("h2")).GetAttribute("innerHTML");
                                        icn_Ment = icn_Ment.Split(new string[] { "<br>" }, StringSplitOptions.None)[1];
                                        icn_Ment = icn_Ment.Split(new string[] { " " }, StringSplitOptions.None)[1];
                                    }
                                    catch { }

                                    // 만약 이용정지 감지 설정 되어있으면
                                    if (user_Test1.Checked)
                                    {
                                        // 경고창에서 담은 변수 텍스트가 '카페' 이면 넘어가고
                                        if (icn_Ment == "카페")
                                        { }
                                        // '아이디가'로 시작되면
                                        else if (icn_Ment == "아이디가")
                                        {
                                            try
                                            {
                                                // 실패한 아이디 개수 늘려주고
                                                id_Failed++;

                                                // 이용정지 id.txt 에 기록
                                                FileStream fs1 = new FileStream(Application.StartupPath + "\\정이용정지id.txt", FileMode.Append, FileAccess.Write);
                                                StreamWriter sw1 = new StreamWriter(fs1, Encoding.UTF8);
                                                sw1.WriteLine(id_Failed + "/" + 작업데이터.Rows.Count.ToString() + "\t" + id + "\t" + pw);
                                                sw1.Close();
                                                fs1.Close();
                                            }
                                            catch { }

                                            // 가입여부는 실패로 설정
                                            result = false;

                                            return result;
                                        }
                                    }
                                }
                            }

                            // 잠김 감지 설정되어있으면
                            if (idcheck1.Checked)
                            {
                                string alert_Text3 = "";
                                try
                                {
                                    alert_Text3 = driver.SwitchTo().Alert().Text;
                                    Thread.Sleep(1153);
                                    driver.SwitchTo().Alert().Dismiss();
                                    Thread.Sleep(1731);
                                }
                                catch { }
                                try
                                {
                                    alert_Text3 = driver.SwitchTo().Alert().Text;
                                    Thread.Sleep(1153);
                                    driver.SwitchTo().Alert().Dismiss();
                                    Thread.Sleep(1731);
                                }
                                catch { }

                                // 로그인 풀릴경우
                                if (driver.FindElements(By.CssSelector("[class='lang_select']")).Count > 0)
                                {
                                    // 로그인 한번더
                                    if (Naver_Login(id, pw, "1", "2"))
                                    { }
                                    else
                                    {
                                        //MessageBox.Show("잠김");
                                        result = false;
                                        return result;
                                    }
                                }
                                // 로그인 문제 없는경우
                                else
                                {
                                    if (driver.FindElements(By.CssSelector("[class='btn btn_primary']")).Count > 0)
                                    {
                                        try
                                        {
                                            FileStream fs2 = new FileStream(Application.StartupPath + "\\방댓카페.txt", FileMode.Append, FileAccess.Write);
                                            StreamWriter sw2 = new StreamWriter(fs2, Encoding.UTF8);
                                            string Curl = driver.FindElement(By.CssSelector("[id='cafeInfo']")).GetAttribute("href");
                                            string VC = driver.FindElement(By.CssSelector("[class='error_content_body']")).GetAttribute("innerHTML");
                                            VC = VC.Split(new string[] { "<br>" }, StringSplitOptions.None)[1];
                                            sw2.WriteLine(Curl + "\t" + VC);
                                            sw2.Close();
                                            fs2.Close();
                                        }
                                        catch { }
                                        Thread.Sleep(1153);
                                    }
                                    else
                                    {
                                        if (driver.FindElements(By.CssSelector("[class='btn btn_normal']")).Count > 0)
                                        {
                                        }
                                        else
                                        {
                                            int menu_Count = 0;
                                            //메뉴화살표누름
                                            try
                                            {
                                                // 게시판 선택
                                                driver.FindElement(By.CssSelector("[class='select_wrap']")).Click();
                                                Thread.Sleep(1700);

                                                // 게시판 수 세고
                                                menu_Count = driver.FindElements(By.CssSelector("[class='radio_label']")).Count();
                                            }
                                            catch { }

                                            // 게시판 수 만큼 루프
                                            for (int k = 0; k < menu_Count; k++)
                                            {
                                                try
                                                {
                                                    // 간편게시판선택해서 취소 한 다음 다시 게시판 누름
                                                    if (driver.FindElements(By.CssSelector("[class='LayerPopup']")).Count == 0)
                                                    {
                                                        // 게시판 선택
                                                        driver.FindElement(By.CssSelector("[class='select_wrap']")).Click();
                                                        Thread.Sleep(1700);
                                                    }
                                                    // k 번째 게시판 클릭
                                                    driver.FindElements(By.CssSelector("[class='radio_label']"))[k].Click();
                                                    Thread.Sleep(1533);
                                                }
                                                catch { }

                                                try
                                                {
                                                    if (driver.FindElements(By.CssSelector("[class='LayerPopup'] [class='message']")).Count > 0)
                                                    {
                                                        string test_Message = driver.FindElement(By.CssSelector("[class='LayerPopup'] [class='message']")).GetAttribute("innerHTML");
                                                        test_Message = test_Message.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                                        if (test_Message == "해당")
                                                        {
                                                            driver.FindElement(By.CssSelector("[class='layer_footer fixed'] [class='ButtonBase ButtonBase--gray']")).Click();
                                                            Sleep_time11();
                                                        }
                                                        // 간편게시판
                                                        if (test_Message == "간편")
                                                        {
                                                            driver.FindElement(By.CssSelector("[class='layer_footer fixed'] [class='ButtonBase ButtonBase--gray']")).Click();
                                                            Sleep_time11();
                                                            continue;
                                                        }
                                                    }
                                                    if (driver.FindElements(By.CssSelector("[class='layer_footer fixed'] [class='ButtonBase ButtonBase--green']")).Count > 0)
                                                    {
                                                        driver.FindElement(By.CssSelector("[class='layer_footer fixed'] [class='ButtonBase ButtonBase--green']")).Click();
                                                        Sleep_time11();
                                                        try
                                                        {
                                                            driver.FindElement(By.CssSelector("[class='se-popup-close-button']")).Click();
                                                            Thread.Sleep(2639);
                                                        }
                                                        catch { }
                                                        break;
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            driver.FindElement(By.CssSelector("[class='se-popup-close-button']")).Click();
                                                            Thread.Sleep(2639);
                                                        }
                                                        catch { }
                                                        break;
                                                    }
                                                }
                                                catch { }

                                            }

                                            // 제목
                                            string input19 = ",./;'ㅜㅠㅏ!~";
                                            Random random = new Random();
                                            int limit9 = random.Next(1, 2);
                                            var chars69 = Enumerable.Range(0, limit9).Select(x => input19[random.Next(0, input19.Length)]);
                                            string ans69 = "";
                                            if (rndText.Checked)
                                            {
                                                ans69 = new string(chars69.ToArray());
                                            }

                                            StreamReader sr2 = new StreamReader(subject + ".txt", Encoding.UTF8, true);
                                            try
                                            {
                                                driver.FindElement(By.CssSelector("[placeholder='제목']")).Click();
                                                driver.FindElement(By.CssSelector("[placeholder='제목']")).SendKeys(sr2.ReadToEnd());
                                                driver.FindElement(By.CssSelector("[placeholder='제목']")).SendKeys(ans69);
                                                Thread.Sleep(733);
                                                sr2.Close();
                                            }
                                            catch { }

                                            // 게시글 복사
                                            Article_Paste();

                                            try
                                            {
                                                string input1 = "ㄱㄴㄷㄹㅁㅂㅅㅇㅈㅊㅋㅌㅍㅎ0123456789";
                                                int limit = random.Next(1, 2);
                                                var chars6 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                                                string ans6 = "";
                                                if (rndText.Checked)
                                                {
                                                    ans6 = new string(chars6.ToArray());
                                                }

                                                /*
                                                try
                                                {
                                                    driver.SwitchTo().Frame(driver.FindElement(By.CssSelector("[id*='input_buffer']")));
                                                }
                                                catch { }
                                                */



                                                try
                                                {
                                                    if (driver.FindElements(By.CssSelector("[class='select_wrap']")).Count > 0)
                                                    {
                                                        try
                                                        {
                                                            driver.SwitchTo().Frame(driver.FindElement(By.CssSelector("[id*='input_buffer']")));
                                                        }
                                                        catch { }

                                                        try
                                                        {
                                                            driver.FindElement(By.CssSelector("body")).SendKeys(OpenQA.Selenium.Keys.Control + "a");
                                                            Thread.Sleep(300);
                                                            driver.FindElement(By.CssSelector("body")).SendKeys(OpenQA.Selenium.Keys.Delete);
                                                            Thread.Sleep(300);
                                                            driver.FindElement(By.CssSelector("body")).SendKeys(OpenQA.Selenium.Keys.Control + "v");
                                                            Thread.Sleep(1300);
                                                            driver.FindElement(By.CssSelector("body")).SendKeys(ans6);
                                                            Thread.Sleep(500);
                                                        }
                                                        catch { }
                                                        try
                                                        {
                                                            driver.SwitchTo().DefaultContent();
                                                        }
                                                        catch { }

                                                        try
                                                        {
                                                            driver.FindElement(By.CssSelector("[class='btn btn_fold'] [class='ButtonBase ButtonBase--white']")).Click();
                                                            Thread.Sleep(433);
                                                        }
                                                        catch { }

                                                        try
                                                        {
                                                            IJavaScriptExecutor js2 = (IJavaScriptExecutor)driver;
                                                            int Yvalue = rnd.Next(1000, 2130);
                                                            string Yvalue1 = Yvalue.ToString();
                                                            js2.ExecuteScript("window.scrollBy(0," + "+" + +Yvalue + ")");
                                                        }
                                                        catch { }

                                                        Sleep_time10();

                                                        if (Search.Checked)
                                                        {
                                                            try
                                                            {
                                                                driver.FindElements(By.CssSelector("[class='bg_track']"))[0].Click();
                                                                Thread.Sleep(433);
                                                            }
                                                            catch { }
                                                        }

                                                        if (Comment.Checked)
                                                        {
                                                            try
                                                            {
                                                                driver.FindElements(By.CssSelector("[class='bg_track']"))[1].Click();
                                                                Thread.Sleep(633);
                                                            }
                                                            catch { }
                                                        }

                                                        if (Share.Checked)
                                                        {
                                                            try
                                                            {
                                                                driver.FindElements(By.CssSelector("[class='bg_track']"))[2].Click();
                                                                Thread.Sleep(633);
                                                            }
                                                            catch { }
                                                            try
                                                            {
                                                                driver.FindElements(By.CssSelector("[class='bg_track']"))[3].Click();
                                                                Thread.Sleep(633);
                                                            }
                                                            catch { }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        continue;
                                                    }
                                                }
                                                catch { }
                                            }
                                            catch { }
                                            Sleep_time10();

                                            // 글 게시 버튼
                                            try
                                            {
                                                driver.FindElement(By.CssSelector("[class='ArticleWriteComplete'] [class='ButtonBase ButtonBase--green']")).Click();
                                                Sleep_time12();
                                            }
                                            catch { }
                                            Sleep_time11();

                                            string alert_Text1 = "";
                                            string alert_Text01 = "";
                                            try
                                            {
                                                try
                                                {
                                                    alert_Text1 = driver.SwitchTo().Alert().Text;
                                                    alert_Text1 = alert_Text1.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                                    if (alert_Text1 == "게시판을")
                                                    {
                                                        driver.SwitchTo().Alert().Accept();
                                                        FileStream fs2 = new FileStream(Application.StartupPath + "\\게시판없음.txt", FileMode.Append, FileAccess.Write);
                                                        StreamWriter sw2 = new StreamWriter(fs2, Encoding.UTF8);
                                                        string Nurl = driver.Url;
                                                        sw2.WriteLine(Nurl);
                                                        sw2.Close();
                                                        fs2.Close();
                                                        Thread.Sleep(1653);
                                                    }
                                                    if (alert_Text1 == "로그인이")
                                                    {
                                                        driver.SwitchTo().Alert().Accept();
                                                        Thread.Sleep(5000);
                                                        //  아디잠김 테스트
                                                        if (driver.FindElements(By.CssSelector("[class='lang_select']")).Count > 0)
                                                        {
                                                            if (Naver_Login(id, pw, "1", "2"))
                                                            { }
                                                            else
                                                            {
                                                                result = false;
                                                                return result;
                                                            }
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }
                                            catch { }

                                            try
                                            {
                                                alert_Text01 = driver.SwitchTo().Alert().Text;
                                                alert_Text01 = alert_Text01.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                                if (alert_Text01 != "게시판을")
                                                {
                                                    driver.SwitchTo().Alert().Accept();
                                                    Thread.Sleep(1653);
                                                }
                                            }
                                            catch { }

                                            try
                                            {
                                                alert_Text01 = driver.SwitchTo().Alert().Text;
                                                alert_Text01 = alert_Text01.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                                if (alert_Text01 != "게시판을")
                                                {
                                                    driver.SwitchTo().Alert().Accept();
                                                    Thread.Sleep(1653);
                                                }
                                            }
                                            catch { }

                                            try
                                            {
                                                alert_Text3 = driver.SwitchTo().Alert().Text;
                                                Thread.Sleep(1153);
                                                driver.SwitchTo().Alert().Dismiss();
                                                Thread.Sleep(1731);
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }
                            //잠김감지 x
                            else
                            {
                                // 1234
                                Thread.Sleep(Convert.ToInt32(Term00.Text) * 1000);
                                if (driver.FindElements(By.CssSelector("[class='btn btn_primary']")).Count > 0)
                                {
                                    try
                                    {
                                        FileStream fs2 = new FileStream(Application.StartupPath + "\\방댓카페.txt", FileMode.Append, FileAccess.Write);
                                        StreamWriter sw2 = new StreamWriter(fs2, Encoding.UTF8);
                                        string Curl = driver.FindElement(By.CssSelector("[id='cafeInfo']")).GetAttribute("href");
                                        string VC = driver.FindElement(By.CssSelector("[class='error_content_body']")).GetAttribute("innerHTML");
                                        VC = VC.Split(new string[] { "<br>" }, StringSplitOptions.None)[1];
                                        sw2.WriteLine(Curl + "\t" + VC);
                                        sw2.Close();
                                        fs2.Close();
                                    }
                                    catch { }
                                    Thread.Sleep(3153);
                                }
                                else
                                {
                                    if (driver.FindElements(By.CssSelector("[class='btn btn_normal']")).Count > 0)
                                    {
                                    }
                                    else
                                    {
                                        int menu_Count = 0;
                                        //메뉴화살표누름
                                        try
                                        {
                                            // 게시판 선택
                                            driver.FindElement(By.CssSelector("[class='selectbox']")).Click();
                                            Thread.Sleep(1700);

                                            // 게시판 수 세고
                                            menu_Count = driver.FindElements(By.CssSelector("[class='radio_label']")).Count();
                                        }
                                        catch { }

                                        // 게시판 수 만큼 루프
                                        for (int k = 0; k < menu_Count; k++)
                                        {
                                            try
                                            {
                                                // 간편게시판선택해서 취소 한 다음 다시 게시판 누름
                                                if (driver.FindElements(By.CssSelector("[class='LayerPopup']")).Count == 0)
                                                {
                                                    // 게시판 선택
                                                    driver.FindElement(By.CssSelector("[class='selectbox']")).Click();
                                                    Thread.Sleep(1700);
                                                }
                                                // k 번째 게시판 클릭
                                                driver.FindElements(By.CssSelector("[class='radio_label']"))[k].Click();
                                                Thread.Sleep(1533);
                                            }
                                            catch { }

                                            try
                                            {
                                                if (driver.FindElements(By.CssSelector("[class='LayerPopup'] [class='message']")).Count > 0)
                                                {
                                                    string test_Message = driver.FindElement(By.CssSelector("[class='LayerPopup'] [class='message']")).GetAttribute("innerHTML");
                                                    test_Message = test_Message.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                                    if (test_Message == "해당")
                                                    {
                                                        driver.FindElement(By.CssSelector("[class='layer_footer fixed'] [class='ButtonBase ButtonBase--gray']")).Click();
                                                        Sleep_time11();
                                                    }
                                                    // 간편게시판
                                                    if (test_Message == "간편")
                                                    {
                                                        driver.FindElement(By.CssSelector("[class='layer_footer fixed'] [class='ButtonBase ButtonBase--gray']")).Click();
                                                        Sleep_time11();
                                                        continue;
                                                    }
                                                }


                                                if (driver.FindElements(By.CssSelector("[class='layer_footer fixed'] [class='ButtonBase ButtonBase--green']")).Count > 0)
                                                {
                                                    driver.FindElement(By.CssSelector("[class='layer_footer fixed'] [class='btn_area'] [class='ButtonBase ButtonBase--green']")).Click();
                                                    Sleep_time11();
                                                    try
                                                    {
                                                        driver.FindElement(By.CssSelector("[class='se-popup-close-button']")).Click();
                                                        Thread.Sleep(2639);
                                                    }
                                                    catch { }                                    
                                                    break;
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        driver.FindElement(By.CssSelector("[class='se-popup-close-button']")).Click();
                                                        Thread.Sleep(2639);
                                                    }
                                                    catch { }
                                                    break;
                                                }
                                            }
                                            catch { }
                                        }

                                        // 제목
                                        string input19 = ",./;'ㅜㅠㅏ!~";
                                        Random random = new Random();
                                        int limit9 = random.Next(1, 2);
                                        var chars69 = Enumerable.Range(0, limit9).Select(x => input19[random.Next(0, input19.Length)]);
                                        string ans69 = "";
                                        if (rndText.Checked)
                                        {
                                            ans69 = new string(chars69.ToArray());
                                        }

                                        StreamReader sr2 = new StreamReader(subject + ".txt", Encoding.UTF8, true);
                                        try
                                        {
                                            driver.FindElement(By.CssSelector("[placeholder='제목']")).Click();
                                            driver.FindElement(By.CssSelector("[placeholder='제목']")).SendKeys(sr2.ReadToEnd());
                                            driver.FindElement(By.CssSelector("[placeholder='제목']")).SendKeys(ans69);
                                            Thread.Sleep(733);
                                            sr2.Close();
                                        }
                                        catch { }

                                        Article_Paste();

                                        try
                                        {
                                            string input1 = "ㄱㄴㄷㄹㅁㅂㅅㅇㅈㅊㅋㅌㅍㅎ0123456789";
                                            int limit = random.Next(1, 2);
                                            var chars6 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                                            string ans6 = "";
                                            if (rndText.Checked)
                                            {
                                                ans6 = new string(chars6.ToArray());
                                            }

                                            /*
                                            try
                                            {
                                                driver.SwitchTo().Frame(driver.FindElement(By.CssSelector("[id*='input_buffer']")));
                                            }
                                            catch { }
                                            */



                                            try
                                            {
                                                if (driver.FindElements(By.CssSelector("[class='select_wrap']")).Count>0)
                                                {
                                                    try
                                                    {
                                                        driver.SwitchTo().Frame(driver.FindElement(By.CssSelector("[id*='input_buffer']")));
                                                    }
                                                    catch { }

                                                    try
                                                    {
                                                        driver.FindElement(By.CssSelector("body")).SendKeys(OpenQA.Selenium.Keys.Control + "a");
                                                        Thread.Sleep(300);
                                                        driver.FindElement(By.CssSelector("body")).SendKeys(OpenQA.Selenium.Keys.Delete);
                                                        Thread.Sleep(300);
                                                        driver.FindElement(By.CssSelector("body")).SendKeys(OpenQA.Selenium.Keys.Control + "v");
                                                        Thread.Sleep(1300);
                                                        driver.FindElement(By.CssSelector("body")).SendKeys(ans6);
                                                        Thread.Sleep(500);
                                                    }
                                                    catch { }
                                                    try
                                                    {
                                                        driver.SwitchTo().DefaultContent();
                                                    }
                                                    catch { }

                                                    try
                                                    {
                                                        driver.FindElement(By.CssSelector("[class='btn btn_fold'] [class='ButtonBase ButtonBase--white']")).Click();
                                                        Thread.Sleep(433);
                                                    }
                                                    catch { }

                                                    try
                                                    {
                                                        IJavaScriptExecutor js2 = (IJavaScriptExecutor)driver;
                                                        int Yvalue = rnd.Next(1000, 2130);
                                                        string Yvalue1 = Yvalue.ToString();
                                                        js2.ExecuteScript("window.scrollBy(0," + "+" + +Yvalue + ")");
                                                    }
                                                    catch { }

                                                    Sleep_time10();

                                                    if (Search.Checked)
                                                    {
                                                        try
                                                        {
                                                            driver.FindElements(By.CssSelector("[class='bg_track']"))[0].Click();
                                                            Thread.Sleep(433);
                                                        }
                                                        catch { }
                                                    }

                                                    if (Comment.Checked)
                                                    {
                                                        try
                                                        {
                                                            driver.FindElements(By.CssSelector("[class='bg_track']"))[1].Click();
                                                            Thread.Sleep(633);
                                                        }
                                                        catch { }
                                                    }

                                                    if (Share.Checked)
                                                    {
                                                        try
                                                        {
                                                            driver.FindElements(By.CssSelector("[class='bg_track']"))[2].Click();
                                                            Thread.Sleep(633);
                                                        }
                                                        catch { }
                                                        try
                                                        {
                                                            driver.FindElements(By.CssSelector("[class='bg_track']"))[3].Click();
                                                            Thread.Sleep(633);
                                                        }
                                                        catch { }
                                                    }
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }
                                            catch { }
                                        }
                                        catch { }
                                        Sleep_time10();
                                    
                                

                                        //글쓰기버튼
                                        try
                                        {
                                            driver.FindElement(By.CssSelector("[class='ArticleWriteComplete'] [class='ButtonBase ButtonBase--green']")).Click();
                                            Sleep_time12();
                                        }
                                        catch { }
                                        Sleep_time11();

                                        string alert_Text1 = "";
                                        string alert_Text01 = "";
                                        try
                                        {
                                            try
                                            {
                                                alert_Text1 = driver.SwitchTo().Alert().Text;
                                                alert_Text1 = alert_Text1.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                                if (alert_Text1 == "게시판을")
                                                {
                                                    driver.SwitchTo().Alert().Accept();
                                                    FileStream fs2 = new FileStream(Application.StartupPath + "\\게시판없음.txt", FileMode.Append, FileAccess.Write);
                                                    StreamWriter sw2 = new StreamWriter(fs2, Encoding.UTF8);
                                                    string Nurl = driver.Url;
                                                    sw2.WriteLine(Nurl);
                                                    sw2.Close();
                                                    fs2.Close();
                                                    Thread.Sleep(1653);
                                                }
                                                if (alert_Text1 == "로그인이")
                                                {
                                                    driver.SwitchTo().Alert().Accept();
                                                    Thread.Sleep(5000);
                                                    result = false;
                                                    return result;
                                                }
                                            }
                                            catch
                                            { }
                                        }
                                        catch
                                        { }

                                        //  아디잠김 테스트
                                        if (driver.FindElements(By.CssSelector("[class='lang_select']")).Count > 0)
                                        {
                                            if (Naver_Login(id, pw, "1", "2"))
                                            { }
                                            else
                                            {
                                                result = false;
                                                return result;
                                            }
                                        }

                                        try
                                        {
                                            alert_Text01 = driver.SwitchTo().Alert().Text;
                                            alert_Text01 = alert_Text01.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                            if (alert_Text01 != "게시판을")
                                            {
                                                driver.SwitchTo().Alert().Accept();
                                                Thread.Sleep(1653);
                                            }
                                        }
                                        catch { }

                                        try
                                        {
                                            alert_Text01 = driver.SwitchTo().Alert().Text;
                                            alert_Text01 = alert_Text01.Split(new string[] { " " }, StringSplitOptions.None)[0];
                                            if (alert_Text01 != "게시판을")
                                            {
                                                driver.SwitchTo().Alert().Accept();
                                                Thread.Sleep(1653);
                                            }
                                        }
                                        catch{ }
                                    }
                                }
                            }
                        }
                        Thread.Sleep(Convert.ToInt32(Term0.Text) * 1000);
                    }
                }
                catch (Exception ex)
                {
                    FileStream fs1 = new FileStream(Application.StartupPath + "\\Error.txt", FileMode.Append, FileAccess.Write);
                    StreamWriter sw1 = new StreamWriter(fs1, Encoding.UTF8);
       
                    sw1.WriteLine(DateTime.Now.ToString("yyyy-MM-dd ") + ex.ToString());
                    sw1.Close();
                    fs1.Close();
                }
            }

            result = true;
            return result;
        }
        void Adb_Send(string arg)
        {
            try
            {
                Process adb_Process = new Process();
                adb_Process.StartInfo.FileName = Application.StartupPath + "\\adb.exe";
                adb_Process.StartInfo.Arguments = arg;
                adb_Process.StartInfo.RedirectStandardOutput = true;
                adb_Process.StartInfo.UseShellExecute = false;
                adb_Process.StartInfo.CreateNoWindow = true;
                adb_Process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                adb_Process.Start();
                adb_Process.WaitForExit();
                adb_Process.Close();
            }
            catch { }
        }
        string Get_IP()
        {
            string result = "";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.ip.pe.kr/");
            request.Method = "GET";
            request.Timeout = 30 * 1000;

            // 추가 
            if (request.RequestUri.Scheme == Uri.UriSchemeHttps)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            }
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                | SecurityProtocolType.Tls11
                                | SecurityProtocolType.Tls12
                                | SecurityProtocolType.Ssl3;

            using (HttpWebResponse resp = (HttpWebResponse)request.GetResponse())
            {
                Stream respStream = resp.GetResponseStream();
                using (StreamReader sr = new StreamReader(respStream))
                {
                    result = sr.ReadToEnd();
                }
            }
            return result;
        }
        void IP_Change()
        {
            //MessageBox.Show("a");
            for (; ; )
            {
                try
                {
                    string old_IP = "";
                    string new_IP = "";
                    old_IP = Get_IP();
                    Adb_Send("shell settings put global airplane_mode_on 1");
                    Adb_Send("shell am broadcast -a android.intent.action.AIRPLANE_MODE");
                    Thread.Sleep(5000);
                    Adb_Send("shell settings put global airplane_mode_on 0");
                    Adb_Send("shell am broadcast -a android.intent.action.AIRPLANE_MODE");
                    Thread.Sleep(15000);
                    new_IP = Get_IP();
                    if (old_IP != new_IP)
                    {
                        break;
                    }
                    else
                    {
                        MessageBox.Show("아이피 변환 실패");
                        continue;
                    }
                }
                catch (Exception Error)
                {
                    Thread.Sleep(10000);
                }
            }
        }
        void IP_Change2()
        {
            /*            try
                        {
                            Adb_Send("shell settings put global airplane_mode_on 1");
                            Adb_Send("shell am broadcast -a android.intent.action.AIRPLANE_MODE");
                            Thread.Sleep(8000);
                            Adb_Send("shell settings put global airplane_mode_on 0");
                            Adb_Send("shell am broadcast -a android.intent.action.AIRPLANE_MODE");
                            Thread.Sleep(15000);
                        }
                        catch (Exception Error)
                        {

                        }*/

            try
            {
                Adb_Send("shell am start -a android.settings.AIRPLANE_MODE_SETTINGS");
                Adb_Send("shell input keyevent 22");
                Adb_Send("shell input keyevent 22");
                Adb_Send("shell input keyevent 23");
                Thread.Sleep(8000);
                Adb_Send("shell input keyevent 23");
                Thread.Sleep(15000);
            }
            catch (Exception e)
            {
                Thread.Sleep(10000);
            }
        }
        void Sleep_time100()
        {
            try
            {
                int sec = 111;
                sec = rnd.Next(50, 150);
                Thread.Sleep(sec);
            }
            catch { }
        }
        void Sleep_time10()
        {
            try
            {
                // 범위내에서 난수 생성해 그만큼 정지
                int sec = 1111;
                sec = rnd.Next(431, 597);
                Thread.Sleep(sec);
            }
            catch { }
        }
        void Sleep_time11()
        {
            try
            {
                int sec = 1111;
                sec = rnd.Next(1831, 2637);
                Thread.Sleep(sec);
            }
            catch { }
        }
        void Sleep_time12()
        {
            try
            {
                int sec = 1111;
                sec = rnd.Next(4131, 6637);
                Thread.Sleep(sec);
            }
            catch { }
        }
        void Sleep_time13()
        {
            try
            {
                int sec = 1111;
                sec = rnd.Next(3131, 14637);
                Thread.Sleep(sec);
            }
            catch { }
        }
        string name_API1()
        {
            string result = "";
            Random random = new Random();
            Random random1 = new Random();
            string LastName = "김김김김김김김김김이이이이이이박박박최최최정정강조윤장임한오서진권황안송전홍유고문양손배조백허유남심노하곽성차주우구신전민진엄지채원천방공강현함변신표탁";

            // 성 랜덤 선택
            var chars41 = Enumerable.Range(1, 1).Select(x => LastName[rnd.Next(0, LastName.Length)]);
            string LastName1 = new string(chars41.ToArray());

            // 성 반환
            result = LastName1;

            return result;
        }
        string captcha_image1()
        {
            BestCaptchaSolverAPI bcs = new BestCaptchaSolverAPI(ACCESS_TOKEN);

            string result = "";
            // account balance
            //string balance = bcs.account_balance();
            //Console.WriteLine(string.Format("Balance: {0}", balance));
            //Console.WriteLine("Solving image captcha ...");

            var d = new Dictionary<string, string>();
            d.Add("image", Application.StartupPath + "\\보안코드.png");
            // d.Add("is_case", "true");       // case sensitive, default: false, optional
            // d.Add("is_phrase", "true");     // contains at least one space, default: false, optional
            // d.Add("is_math", "true");       // math calculation captcha, default: false, optional
            // d.Add("alphanumeric", "2");     // 1 (digits only) or 2 (letters only), default: all characters
            // d.Add("minlength", "3");        // minimum length of captcha text, default: any
            // d.Add("maxlength", "4");        // maximum length of captcha text, default: any
            // d.Add("affiliate_id", "get it from /account");      // affiliate ID
            var id = bcs.submit_image_captcha(d);
            string image_text = "";
            while (image_text == "")
            {
                image_text = bcs.retrieve(id)["text"];
                //Thread.Sleep(5000);
            }
            //result = string.Format("Captcha text: {0}", image_text);
            result = image_text;
            return result;
            // bcs.set_captcha_bad(id);      // set captcha as bad
        }
        string Random_Nick()
        {
            // 랜덤닉 만듬
            Random random = new Random();
            Random random1 = new Random();

            string[] maleNames = new string[] { "대언", "대연", "대영", "대원", "대윤", "대은", "대율", "대인", "대한", "대현", "대형", "대환", "대훈", "대경", "대권", "대규", "대융", "대우", "대후", "다헌", "다형", "다환", "다훈", "도연", "도영", "도원", "도윤", "도율", "도헌", "도현", "도훈", "동언", "동연", "동영", "동예", "동완", "동원", "동운", "동윤", "동은", "동율", "동인", "동한", "동해", "동헌", "동현", "동혜", "동환", "동훈", "동희", "동율", "동후", "동우", "두영", "두윤", "두율", "두환", "두훈", "래원", "래헌", "래환", "래훈", "태연", "태영", "태원", "태윤", "태은", "태율", "태한", "태헌", "태현", "태환", "태훈", "태율", "류원", "윤관", "윤교", "윤규", "윤기", "윤겸", "윤렬", "윤태", "은교", "은겸", "은규", "은률", "은린", "한영", "한율", "한오", "한준", "한성", "연우", "연욱", "영인", "우원", "원영", "원오", "원우", "원일", "윤열", "윤오", "윤용", "윤우", "윤일", "은우", "은오", "은율", "하원", "하율", "하윤", "하일", "하은", "현호", "현우", "현일", "원훈", "원형", "원호", "원혁", "유한", "유현", "유형", "유환", "윤하", "윤한", "윤현", "윤헌", "윤형", "윤호", "윤혁", "윤해", "윤후", "이헌", "이한", "효헌", "연서", "우성", "유상", "유성", "유신", "윤상", "윤성", "윤서", "윤세", "윤섭", "윤수", "은서", "은상", "은세", "은성", "은수", "은섭", "하성", "한상", "한수", "효상", "효성", "희상", "희성", "희수", "아준", "여준", "연재", "영준", "우재", "우준", "우진", "원재", "원정", "원준", "유준", "유진", "윤재", "윤제", "윤준", "윤찬", "유찬", "윤진", "은재", "은준", "은찬", "은진", "이준", "이찬", "하준", "하진", "효준", "효찬", "효재", "영재", "영찬", "영준", "원찬", "유창", "윤채", "윤철", "은찬", "은채", "의찬", "현서", "은후", "연후", "윤후", "현후", "영후", "현준", "예준", "예찬", "영준", "영찬", "우찬", "원준-이안", "이완", "이헌", "이훈", "이준", "이찬", "이수", "이호", "이황", "상아", "상연", "상영", "상완", "상우", "상원", "상윤", "", "상헌", "상훈", "상현", "상호", "상환", "상후", "상희", "상율", "상일", "상엽", "서훈", "서환", "서후", "서준", "서진", "서빈", "성연", "성영", "성완", "성우", "성원", "성윤", "성헌", "성현", "성훈", "성호", "성후", "성희", "성율", "성하", "성한", "성은", "성일", "성혁", "성엽", "성수", "성재", "성진", "성준", "성찬", "성민", "성빈", "세연", "세영", "세은", "세완", "세원", "세윤", "세현", "세훈", "세호", "세후", "세율", "세희", "세한", "세일", "세혁", "세준", "세진", "세민", "세빈", "세명", "송훈", "송후", "송혁", "송민", "송빈", "수훈", "수헌", "수한", "수혁", "수성", "수민", "수빈", "승연", "승현", "승완", "승우", "승원", "승윤", "승헌", "승훈", "승한", "승호", "승환", "승후", "승일", "승엽", "승혁", "승진", "승준", "승민", "승빈", "시현", "시훈", "시윤", "시환", "시율", "시우", "시원", "시후", "시혁", "시헌", "시진", "시준", "시민", "시빈", "장연", "장완", "장원", "장우", "장윤", "장헌", "장훈", "장우", "장현", "장호", "장혁", "재연", "재영", "재우", "재원", "재헌", "재훈", "재현", "재호", "재환", "재율", "재일", "재혁", "재준", "재진", "재찬", "재성", "재민", "재빈", "조현", "조영", "조원", "종연", "종완", "종우", "종원", "종윤", "종헌", "종훈", "종현", "종호", "종환", "종후", "종한", "종일", "종혁", "종수", "종성", "종찬", "종민", "종재", "종빈", "종명", "주원", "주호", "주환", "주한", "주헌", "주훈", "주혁", "주찬", "주성", "주빈", "준영", "준아", "준연", "준완", "준우", "준원", "준현", "준헌", "준호", "준후", "준일", "준혁", "준상", "준성", "지운", "지완", "지한", "지헌", "주훈", "지율", "지환", "지호", "지후", "지혁", "지민", "지빈", "지명", "진호", "진영", "진혁", "차헌", "차훈", "차민", "차빈", "찬영", "찬연", "찬우", "찬원", "찬헌", "찬현", "찬호", "찬후", "찬율", "찬혁", "명진", "명성", "명세", "명민", "명재", "명제", "명준", "명찬", "민서", "민준", "민찬", "민건", "민겸", "민국", "민관", "민규", "민기", "민상", "민세", "민성", "민서", "민준", "민찬", "민건", "민겸", "민국", "민관", "민규", "민기", "민상", "민세", "민성", "범성", "범찬", "범교", "범준", "범규", "범기", "범상", "범세", "범창" };
            string LastName = "김김김김김김김김김이이이이이이박박박최최최정정강조윤장임한오서진권황안송전홍유고문양손배조백허유남심노하곽성차주우구신전민진엄지채원천방공강현함변신표탁";
            
            // 이름
            string FirstName = "";
            FirstName = maleNames[rnd.Next(0, maleNames.Length - 1)];
            
            // 성
            var chars41 = Enumerable.Range(1, 1).Select(x => LastName[rnd.Next(0, LastName.Length)]);
            string LastName1 = new string(chars41.ToArray());

            string result = "";
            int j_random0 = 0;
            string go_API = "";
            string go_Nickname = "";

            if (j_random0 == 0)
            {
                go_Nickname = LastName1 + FirstName;
                result = go_Nickname;
            }
            else if (j_random0 == 1)
            {
                //2글자 + 이름
                go_API = "7nickname_27";
            }
            else if (j_random0 == 2)
            {
                //2글자 + 2글자
                go_API = "7nickname_27";
            }
            else if (j_random0 == 3)
            {
                //3글자 + 이름
                go_API = "7nickname_37";
            }
            return result;
        }
       
        string Fixed_Nick()
        {
            string result = "";
            string[] fix_nickname = File.ReadAllLines(Application.StartupPath + "\\고정닉네임.txt", Encoding.UTF8);

            Random rnd = new Random();
            int num = rnd.Next(100, 999);
            int m = rnd.Next(0, (fix_nickname.Count() - 1));

            result = fix_nickname[m] + num.ToString();

            return result;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            /*
            string path = Application.StartupPath + "\\계정.txt";
            아디비번창고.Rows.Clear();
            StreamReader sr = new StreamReader(path, Encoding.Default, true);
            string load_Data_All = sr.ReadToEnd();
            for (int i = 0; i < load_Data_All.Split(new string[] { "\n" }, StringSplitOptions.None).Count(); i++)
            {
                string load_Date = load_Data_All.Split(new string[] { "\r\n" }, StringSplitOptions.None)[i];
                if (load_Date != "")
                {
                    아디비번창고.Rows.Add(false, load_Date.Split(new string[] { "\t" }, StringSplitOptions.None)[0], load_Date.Split(new string[] { "\t" }, StringSplitOptions.None)[1]);
                }
            }
            sr.Close();
            */
        }
        private void idsave_Click(object sender, EventArgs e)
        {
            /*string path = Application.StartupPath + "\\계정.txt";
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, Encoding.Default);

            for (int i = 0; i< 아디비번창고.Rows.Count - 1; i++)
            {
                sw.WriteLine(아디비번창고.Rows[i].Cells[1].Value + "\t" + 아디비번창고.Rows[i].Cells[2].Value);
            }
            sw.Close();
            fs.Close();*/
        }

        void TakeScreenshot(IWebElement element, string fileName)
        {
            // 해당 엘리멘트로 스클로 이동
            var actions = new Actions(driver);
            actions.MoveToElement(element);
            actions.Perform();
            // 해당 엘리멘트의 좌표 추출
            //var locationWhenScrolled = ((OpenQA.Selenium.Remote.RemoteWebElement)element).LocationOnScreenOnceScrolledIntoView;
            var locationWhenScrolled = ((OpenQA.Selenium.WebElement)element).LocationOnScreenOnceScrolledIntoView;
            var byteArray = ((ITakesScreenshot)driver).GetScreenshot().AsByteArray;
            using (var screenshot = new System.Drawing.Bitmap(new System.IO.MemoryStream(byteArray)))
            {
                var location = locationWhenScrolled;
                // OutOfMemory Exception 해결을 위한 코드
                if (location.X + element.Size.Width > screenshot.Width)
                {
                    location.X = screenshot.Width - element.Size.Width;
                }
                if (location.Y + element.Size.Height > screenshot.Height)
                {
                    location.Y = screenshot.Height - element.Size.Height;
                }
                // 부분 추출
                var croppedImage = new System.Drawing.Rectangle(location.X, location.Y, element.Size.Width, element.Size.Height);
                using (var clone = screenshot.Clone(croppedImage, screenshot.PixelFormat))
                {
                    clone.Save(fileName, ImageFormat.Png);
                }
            }
        }
        void TakeScreenshot2(IWebElement element, string fileName)
        {
            // 해당 엘리멘트로 스클로 이동
            /*
            var actions = new Actions(driver);
            actions.MoveToElement(element);
            actions.Perform();
            */
            // 해당 엘리멘트의 좌표 추출
            var locationWhenScrolled = ((RemoteWebElement)element).LocationOnScreenOnceScrolledIntoView;
            var byteArray = ((ITakesScreenshot)driver).GetScreenshot().AsByteArray;
            using (var screenshot = new System.Drawing.Bitmap(new System.IO.MemoryStream(byteArray)))
            {
                var location = locationWhenScrolled;
                // OutOfMemory Exception 해결을 위한 코드
                if (location.X + element.Size.Width > screenshot.Width)
                {
                    location.X = screenshot.Width - element.Size.Width;
                }
                if (location.Y + element.Size.Height > screenshot.Height)
                {
                    location.Y = screenshot.Height - element.Size.Height;
                }
                // 부분 추출
                var croppedImage = new System.Drawing.Rectangle(location.X, location.Y, element.Size.Width, element.Size.Height);
                using (var clone = screenshot.Clone(croppedImage, screenshot.PixelFormat))
                {
                    clone.Save(fileName, ImageFormat.Png);
                }
            }
        }
        void Ndb_Process()
        {
            Random random2 = new Random();
            int sec2 = 1000;
            string ndb_Name = "";
            string play_Role = "";
            int point_True = 0;

            string place_Addr = ndb_addr();
            //System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(place_Addr, "----");
            //int cnt1 = matches.Count;
            string[] addr0 = place_Addr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < addr0.Count(); i++)
            {
                string place_Addr0 = addr0[i].Split(new string[] { "-----" }, StringSplitOptions.None)[0];
                string place_Addr1 = addr0[i].Split(new string[] { "-----" }, StringSplitOptions.None)[1];
                bool Place_True;
                bool Cafe_True;
                Cafe_True = place_Addr0.Contains("cafe");
                if (Cafe_True)
                {
                    //카페
                    play_Role = "CF";
                }
                else
                {
                    //플레이스
                    play_Role = "PL";
                }

                if (play_Role == "PL")
                {
                    ndb_Name = place_Addr1;
                    string ndb_Url = "https://m.map.naver.com/search2/search.nhn?query=" + ndb_Name;

                    try
                    {
                        driver.Navigate().GoToUrl(ndb_Url);

                        sec2 = random2.Next(1000 * 4, 1000 * 8);
                        Thread.Sleep(sec2);

                        driver.FindElement(By.CssSelector("[class*='u_likeit_list_btn']")).Click();
                        sec2 = random2.Next(1000 * 1, 1000 * 2);
                        Thread.Sleep(sec2);

                        try
                        {
                            IAlert alert = driver.SwitchTo().Alert();
                            alert.Accept();
                            Thread.Sleep(1500);
                        }
                        catch { }
                        try
                        {
                            IAlert alert = driver.SwitchTo().Alert();
                            alert.Accept();
                            Thread.Sleep(1500);
                        }
                        catch { }

                        driver.FindElement(By.CssSelector("[class*='a_item a_item']")).Click();
                        sec2 = random2.Next(1000 * 5, 1000 * 7);
                        Thread.Sleep(sec2);


                        driver.FindElements(By.CssSelector("[class*='_3EHfkvyTQU']"))[2].Click();
                        sec2 = random2.Next(1000 * 3, 1000 * 5);
                        Thread.Sleep(sec2);

                        driver.FindElements(By.CssSelector("[type='button']"))[2].Click();
                        sec2 = random2.Next(1000 * 3, 1000 * 5);
                        Thread.Sleep(sec2);

                        point_True += 70;
                    }
                    catch { }
                }
                else if (play_Role == "CF")
                {
                    ndb_Name = place_Addr1;
                    string ndb_Url = "https://m.cafe.naver.com/" + ndb_Name;
                    driver.Navigate().GoToUrl(ndb_Url);
                    sec2 = random2.Next(1000 * 3, 1000 * 5);
                    Thread.Sleep(sec2);

                    try
                    {
                        driver.FindElement(By.CssSelector("[class='ButtonBase ButtonBase--gray']")).Click();
                        Thread.Sleep(1711);
                    }
                    catch { }

                    if (driver.FindElements(By.CssSelector("[class='btn_join']")).Count > 0)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            try
                            {
                                driver.FindElement(By.CssSelector("[class='btn_join']")).Click();
                                Thread.Sleep(3000);
                                break;
                            }
                            catch
                            {
                                Thread.Sleep(1000);
                            }
                        }
                        //driver.Navigate().GoToUrl(driver.FindElement(By.CssSelector("[class='btn_join']")).GetAttribute("href"));


                        string ans1 = "";
                        string ans2 = "";
                        string ans3 = "";
                        string ans4 = "";
                        string ans5 = "";
                        string ans6 = "";
                        Random random = new Random();
                        int limit = 3;

                        string input1 = "abcdefghijklmnopqrstuvwxyz0123456789";

                        string[] maleNames = new string[] { "대언", "대연", "대영", "대원", "대윤", "대은", "대율", "대인", "대한", "대현", "대형", "대환", "대훈", "대경", "대권", "대규", "대융", "대우", "대후", "다헌", "다형", "다환", "다훈", "도연", "도영", "도원", "도윤", "도율", "도헌", "도현", "도훈", "동언", "동연", "동영", "동예", "동완", "동원", "동운", "동윤", "동은", "동율", "동인", "동한", "동해", "동헌", "동현", "동혜", "동환", "동훈", "동희", "동율", "동후", "동우", "두영", "두윤", "두율", "두환", "두훈", "래원", "래헌", "래환", "래훈", "태연", "태영", "태원", "태윤", "태은", "태율", "태한", "태헌", "태현", "태환", "태훈", "태율", "류원", "윤관", "윤교", "윤규", "윤기", "윤겸", "윤렬", "윤태", "은교", "은겸", "은규", "은률", "은린", "한영", "한율", "한오", "한준", "한성", "연우", "연욱", "영인", "우원", "원영", "원오", "원우", "원일", "윤열", "윤오", "윤용", "윤우", "윤일", "은우", "은오", "은율", "하원", "하율", "하윤", "하일", "하은", "현호", "현우", "현일", "원훈", "원형", "원호", "원혁", "유한", "유현", "유형", "유환", "윤하", "윤한", "윤현", "윤헌", "윤형", "윤호", "윤혁", "윤해", "윤후", "이헌", "이한", "효헌", "연서", "우성", "유상", "유성", "유신", "윤상", "윤성", "윤서", "윤세", "윤섭", "윤수", "은서", "은상", "은세", "은성", "은수", "은섭", "하성", "한상", "한수", "효상", "효성", "희상", "희성", "희수", "아준", "여준", "연재", "영준", "우재", "우준", "우진", "원재", "원정", "원준", "유준", "유진", "윤재", "윤제", "윤준", "윤찬", "유찬", "윤진", "은재", "은준", "은찬", "은진", "이준", "이찬", "하준", "하진", "효준", "효찬", "효재", "영재", "영찬", "영준", "원찬", "유창", "윤채", "윤철", "은찬", "은채", "의찬", "현서", "은후", "연후", "윤후", "현후", "영후", "현준", "예준", "예찬", "영준", "영찬", "우찬", "원준-이안", "이완", "이헌", "이훈", "이준", "이찬", "이수", "이호", "이황", "상아", "상연", "상영", "상완", "상우", "상원", "상윤", "", "상헌", "상훈", "상현", "상호", "상환", "상후", "상희", "상율", "상일", "상엽", "서훈", "서환", "서후", "서준", "서진", "서빈", "성연", "성영", "성완", "성우", "성원", "성윤", "성헌", "성현", "성훈", "성호", "성후", "성희", "성율", "성하", "성한", "성은", "성일", "성혁", "성엽", "성수", "성재", "성진", "성준", "성찬", "성민", "성빈", "세연", "세영", "세은", "세완", "세원", "세윤", "세현", "세훈", "세호", "세후", "세율", "세희", "세한", "세일", "세혁", "세준", "세진", "세민", "세빈", "세명", "송훈", "송후", "송혁", "송민", "송빈", "수훈", "수헌", "수한", "수혁", "수성", "수민", "수빈", "승연", "승현", "승완", "승우", "승원", "승윤", "승헌", "승훈", "승한", "승호", "승환", "승후", "승일", "승엽", "승혁", "승진", "승준", "승민", "승빈", "시현", "시훈", "시윤", "시환", "시율", "시우", "시원", "시후", "시혁", "시헌", "시진", "시준", "시민", "시빈", "장연", "장완", "장원", "장우", "장윤", "장헌", "장훈", "장우", "장현", "장호", "장혁", "재연", "재영", "재우", "재원", "재헌", "재훈", "재현", "재호", "재환", "재율", "재일", "재혁", "재준", "재진", "재찬", "재성", "재민", "재빈", "조현", "조영", "조원", "종연", "종완", "종우", "종원", "종윤", "종헌", "종훈", "종현", "종호", "종환", "종후", "종한", "종일", "종혁", "종수", "종성", "종찬", "종민", "종재", "종빈", "종명", "주원", "주호", "주환", "주한", "주헌", "주훈", "주혁", "주찬", "주성", "주빈", "준영", "준아", "준연", "준완", "준우", "준원", "준현", "준헌", "준호", "준후", "준일", "준혁", "준상", "준성", "지운", "지완", "지한", "지헌", "주훈", "지율", "지환", "지호", "지후", "지혁", "지민", "지빈", "지명", "진호", "진영", "진혁", "차헌", "차훈", "차민", "차빈", "찬영", "찬연", "찬우", "찬원", "찬헌", "찬현", "찬호", "찬후", "찬율", "찬혁", "명진", "명성", "명세", "명민", "명재", "명제", "명준", "명찬", "민서", "민준", "민찬", "민건", "민겸", "민국", "민관", "민규", "민기", "민상", "민세", "민성", "민서", "민준", "민찬", "민건", "민겸", "민국", "민관", "민규", "민기", "민상", "민세", "민성", "범성", "범찬", "범교", "범준", "범규", "범기", "범상", "범세", "범창" };
                        string FirstName = "";
                        Random rand = new Random(DateTime.Now.Second);
                        FirstName = maleNames[rand.Next(0, maleNames.Length - 1)];

                        Random random3 = new Random();
                        string LastName = "김김김김김김김김김이이이이이이박박박최최최정정강조윤장임한오서진권황안송전홍유고문양손배조백허유남심노하곽성차주우구신전민진엄지채원천방공강현함변신표탁";
                        var chars41 = Enumerable.Range(1, 1).Select(x => LastName[random2.Next(0, LastName.Length)]);
                        string name31 = new string(chars41.ToArray());
                        string ans60 = name31 + FirstName;

                        limit = random.Next(2, 6);
                        int limit2 = 2;
                        var chars1 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        limit = random.Next(2, 6);
                        var chars2 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        limit = random.Next(2, 6);
                        var chars3 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        limit = random.Next(2, 6);
                        var chars4 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        limit = random.Next(2, 6);
                        var chars5 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        limit = random.Next(2, 6);
                        var chars6 = Enumerable.Range(0, limit).Select(x => input1[random.Next(0, input1.Length)]);
                        ans1 = new string(chars1.ToArray());
                        ans2 = new string(chars2.ToArray());
                        ans3 = new string(chars3.ToArray());
                        ans4 = new string(chars4.ToArray());
                        ans5 = new string(chars5.ToArray());
                        ans6 = new string(chars6.ToArray());

                        int sec = 0;

                        if (NName.Checked)
                        {
                            try
                            {
                                if (i == 0)
                                {
                                    Thread.Sleep(2231);
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Clear();
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(ans60 + ans6);
                                    Thread.Sleep(2371);
                                }
                                else if (i == 1)
                                {
                                    Thread.Sleep(2231);
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Clear();
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(ans60);
                                    Thread.Sleep(2371);
                                }
                                else if (i == 2)
                                {
                                    Thread.Sleep(2231);
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Clear();
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(ans60);
                                    Thread.Sleep(2371);
                                }
                            }
                            catch { }
                        }

                        try
                        {
                            if (driver.FindElements(By.CssSelector("[class='input_message alert']")).Count > 0)
                            {
                                for (int k = 0; k < 3; k++)
                                {
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(ans6);
                                    Thread.Sleep(1771);
                                    if (driver.FindElements(By.CssSelector("[class='input_message']")).Count > 0)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }

                        try
                        {
                            driver.FindElement(By.CssSelector("[for='radio_join_question_1_0']")).Click();
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[for='radio_join_question_2_0']")).Click();
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[for='radio_join_question_3_0']")).Click();
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[for='radio_join_question_4_0']")).Click();
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[for='radio_join_question_5_0']")).Click();
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_1']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_1']")).SendKeys(ans1);
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_2']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_2']")).SendKeys(ans2);
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_3']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_3']")).SendKeys(ans3);
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_4']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_4']")).SendKeys(ans4);
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_5']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_5']")).SendKeys(ans5);
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='label_join_question_6']")).Click();
                            driver.FindElement(By.CssSelector("[id='label_join_question_6']")).SendKeys(ans6);
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='applyAnswer7']")).Click();
                            driver.FindElement(By.CssSelector("[id='applyAnswer7']")).SendKeys(ans6 + "e");
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='applyAnswer8']")).Click();
                            driver.FindElement(By.CssSelector("[id='applyAnswer8']")).SendKeys("f" + ans5 + "w");
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        try
                        {
                            driver.FindElement(By.CssSelector("[id='applyAnswer9']")).Click();
                            driver.FindElement(By.CssSelector("[id='applyAnswer9']")).SendKeys("a" + ans5);
                            Random random1 = new Random();
                            sec = random1.Next(531, 1633);
                            Thread.Sleep(sec);
                        }
                        catch { }
                        /*
                        for(int i = 1; i < 5 ; i++)
                        {
                            if(driver.FindElements(By.CssSelector("[id='applyAnswer" + i + "']")).Count > 0)
                            {
                                // 질문
                            }
                            else
                            {
                                break;
                            }
                        }
                        */
                        if (driver.FindElements(By.CssSelector("[class='chaptcha_img']")).Count > 0)
                        {
                            for (int z = 0; z < 5; z++)
                            {
                                try
                                {
                                    FastSolver.FastSolver fastSolver = new FastSolver.FastSolver(Fid, Fps);
                                    if (fastSolver.Login())
                                    {
                                        if (File.Exists(Application.StartupPath + "\\화면캡쳐.png"))
                                        {
                                            File.Delete(Application.StartupPath + "\\화면캡쳐.png");
                                        }
                                        if (File.Exists(Application.StartupPath + "\\보안코드.png"))
                                        {
                                            File.Delete(Application.StartupPath + "\\보안코드.png");
                                        }
                                        try
                                        {
                                            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                                            js.ExecuteScript("arguments[0].scrollIntoView();", driver.FindElement(By.CssSelector("[class='chaptcha_img']")));
                                            Thread.Sleep(519);
                                        }
                                        catch { }
                                        try
                                        {
                                            for (int x = 0; x < 5; x++)
                                            {
                                                new Actions(driver).MoveToElement(driver.FindElement(By.CssSelector("[class='chaptcha_img']"))).Perform();
                                                Thread.Sleep(100);
                                            }
                                        }
                                        catch { }
                                        try
                                        {
                                            if (z > 2)
                                            {
                                                driver.FindElement(By.CssSelector("[id='label_join_captcha']")).Click();
                                            }
                                        }
                                        catch { }

                                        TakeScreenshot(driver.FindElement(By.CssSelector("[class='chaptcha_img']")), Application.StartupPath + "\\보안코드.png");

                                        string code = fastSolver.Image_Upload(Application.StartupPath + "\\보안코드.png", "1");
                                        try
                                        {
                                            if (fastSolver.Result_State(code) == "success")
                                            {
                                                driver.FindElement(By.CssSelector("[id='label_join_captcha']")).Click();
                                                driver.FindElement(By.CssSelector("[id='label_join_captcha']")).SendKeys(fastSolver.Result_Value(code));
                                                driver.FindElement(By.CssSelector("[class='join_btn_box']")).Click();
                                                Thread.Sleep(1931);
                                                if (driver.FindElements(By.CssSelector("[class='chaptcha_img']")).Count == 0)
                                                {
                                                    bool result = true;
                                                    break;
                                                }
                                                if (driver.FindElements(By.CssSelector("[class='input_message alert']")).Count == 1)
                                                {
                                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                                    driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(ans6);
                                                    Thread.Sleep(100);
                                                }
                                                if (driver.FindElements(By.CssSelector("[class='chaptcha_img']")).Count == 1)
                                                {
                                                    driver.FindElement(By.CssSelector("[id='label_join_captcha']")).Clear();
                                                    new Actions(driver).MoveToElement(driver.FindElement(By.CssSelector("[class='cafe_name']"))).Perform();
                                                    Thread.Sleep(100);
                                                }

                                            }
                                            else
                                            {
                                                driver.FindElement(By.CssSelector("[class='chaptcha_btn']")).Click();
                                                Thread.Sleep(3000);
                                            }
                                        }
                                        catch { }
                                    }
                                    else
                                    {
                                    }
                                }
                                catch { }
                            }
                        }
                        else
                        {
                            if (driver.FindElements(By.CssSelector("[class='join_btn_box']")).Count > 0)
                            {
                                driver.FindElement(By.CssSelector("[class='join_btn_box']")).Click();
                            }
                            else
                            {
                            }

                        }

                        // 이미지캡쳐 삭제
                        try
                        {
                            driver.FindElement(By.CssSelector("[class='join_btn_box']")).Click();
                        }
                        catch { }
                        Thread.Sleep(2231);
                        string alert_Text = "";
                        try
                        {
                            alert_Text = driver.SwitchTo().Alert().Text;
                            try
                            {
                                alert_Text = alert_Text.Split(new string[] { " " }, StringSplitOptions.None)[0];
                            }
                            catch { }
                            if (alert_Text != "카페내")
                            {
                                driver.SwitchTo().Alert().Accept();
                                Thread.Sleep(2231);
                                driver.FindElement(By.CssSelector("[placeholder='별명']")).Click();
                                driver.FindElement(By.CssSelector("[placeholder='별명']")).SendKeys(ans6);
                                Thread.Sleep(2371);
                                driver.FindElement(By.CssSelector("[class='join_btn_box']")).Click();
                                Thread.Sleep(1371);

                                //가입실패
                            }
                        }
                        catch { }
                        try
                        {
                            driver.Navigate().GoToUrl(ndb_Url);
                            sec2 = random2.Next(1000 * 3, 1000 * 5);
                            Thread.Sleep(sec2);
                            if (driver.FindElements(By.CssSelector("[class='btns_area']")).Count > 0)
                            {
                                point_True += 30;
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        if (driver.FindElements(By.CssSelector("[class='btn_write']")).Count == 0)
                        {
                            FileStream fs2 = new FileStream(Application.StartupPath + "\\가입불가.txt", FileMode.Append, FileAccess.Write);
                            StreamWriter sw2 = new StreamWriter(fs2, Encoding.UTF8);
                            string Nurl = driver.Url;
                            sw2.WriteLine(Nurl);
                            sw2.Close();
                            fs2.Close();
                            Thread.Sleep(653);
                        }
                    }
                }
            }

            if (point_True > 1)
            {
                string data = point_True.ToString();
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://45.32.31.200/api/?name=" + Fid + "&data=" + data);
                    request.Method = "POST";

                    using (WebResponse response = request.GetResponse())
                    {
                        HttpWebRequest request2 = (HttpWebRequest)WebRequest.Create("http://45.32.31.200/api/?name=" + Fid);
                        request2.Method = "GET";
                        using (WebResponse response2 = request2.GetResponse())
                        {
                            //using (StreamReader reader = new StreamReader(response2.GetResponseStream()))
                            //{

                            //}
                        }
                    }
                }
                catch
                {
                    FileStream fs = new FileStream(Application.StartupPath + "\\실패.txt", FileMode.Append, FileAccess.Write);
                    StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                    sw.WriteLine("실패");
                    sw.Close();
                    fs.Close();
                    string error_Name = "error";
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://45.32.31.200/api/?name=" + Fid + "&data=" + "0");
                        request.Method = "POST";

                        using (WebResponse response = request.GetResponse())
                        {

                        }
                    }
                    catch { }
                }
            }
        }
        string ndb_addr()
        {
            string place_addr = "";
            string addr = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://45.32.31.200/api/addr.php");
            request.Method = "GET";

            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        addr = reader.ReadToEnd();
                        string[] addr0 = addr.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < addr0.Count(); i++)
                        {
                            string go_Addr0 = addr0[i].Split(new string[] { "-----" }, StringSplitOptions.None)[0];
                            string go_Addr1 = addr0[i].Split(new string[] { "-----" }, StringSplitOptions.None)[1];
                            place_addr = go_Addr1;
                        }
                        //string[] addr = reader.ReadToEnd().ToCharArray().Select(c => c.ToString()).ToArray();
                        //string addr1 = addr[0];
                        //string addr2 = addr[1];
                        //MessageBox.Show(reader.ReadToEnd());
                    }
                }
            }
            catch
            {
                FileStream fs = new FileStream(Application.StartupPath + "\\실패.txt", FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                sw.WriteLine("실패");
                sw.Close();
                fs.Close();
            }
            //MessageBox.Show(addr);
            return addr;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + "\\Account.txt")) // 중복 저장을 피하기 위해 존재한다면 삭제
            {
                File.Delete(Application.StartupPath + "\\Account.txt");
            }
            for (int i = 0; i < 작업데이터.Rows.Count - 1; i++)
            {
                FileStream fs = new FileStream(Application.StartupPath + "\\Account.txt", FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                sw.WriteLine(Convert.ToBoolean(작업데이터.Rows[i].Cells[0].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[1].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[2].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[3].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[4].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[5].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[6].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[7].Value) + "\t" + Convert.ToString(작업데이터.Rows[i].Cells[8].Value));
                sw.Close();
                fs.Close();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string[] account = File.ReadAllLines(Application.StartupPath + "\\Account.txt", Encoding.UTF8);
            for (int i = 0; i < account.Count(); i++)
            {
                작업데이터.Rows.Add(account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[0], account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[1], account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[2], account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[3], account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[4], account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[5], account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[6], account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[7], account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[8]);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string[] account = File.ReadAllLines(Application.StartupPath + "\\성공id.txt", Encoding.UTF8);
            for (int i = 0; i < account.Count(); i++)
            {
                작업데이터.Rows.Add(true, account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[0], account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[1], S1.Text, S2.Text, S3.Text, "", account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[2], account[i].Split(new string[] { "\t" }, StringSplitOptions.None)[3]);
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
