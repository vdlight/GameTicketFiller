


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Input;


namespace GameTicketFiller
{

    public partial class Form1 : Form
    {
        int currSvenskaSpelX = -1;
        int currSvenskaSpelY = -1;
        int currCTX = -1;
        int currCTY = -1;

        uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        uint MOUSEEVENTF_LEFTUP = 0x0004;

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("User32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern short GetKeyState(int nVirtKey);

        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr dc, int x, int y);

        [DllImport("user32.dll")]
        public static extern void SetPixel(int X, int Y, Color color);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);




        bool grabMousePosNextClick;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        Point pt;
        int waitTimer = 0;
        

        enum commitEnum{
            wait,
            activateNextCupon,
            fillInRows,
            readInTickets,
            idle,
            fillInRow,
            expandPay,
            fillInTicket,
            fillInTickets,
            confirm,
            pay,
            afterPayAck,
            verifyTicket,
        };

        string[] signs = { "1", "X", "2" };

        string[] commitEnumString = { "Wait", "Activate next cupon", "Fill in rows", "Read in rows","Idle", "Fill in row"};


        commitEnum next_commit_state = commitEnum.idle;
        commitEnum commit_state = commitEnum.idle;
        int currentRowToFill = 0;
        int ticketTimer = 0;
        int currentTimePerTicket = 0;




        private void timer_50ms_Tick(object sender, EventArgs e)
        {

        Label[,] labels = new Label[,] {
                { lbl1_1, lbl1_X, lbl1_2},
                { lbl2_1, lbl2_X, lbl2_2},
                { lbl3_1, lbl3_X, lbl3_2},
                { lbl4_1, lbl4_X, lbl4_2},
                { lbl5_1, lbl5_X, lbl5_2},
                { lbl6_1, lbl6_X, lbl6_2},
                { lbl7_1, lbl7_X, lbl7_2},
                { lbl8_1, lbl8_X, lbl8_2}
            };

            Label[,] labelsCT = new Label[,] {
                { lbl1_1_CT, lbl1_X_CT, lbl1_2_CT},
                { lbl2_1_CT, lbl2_X_CT, lbl2_2_CT},
                { lbl3_1_CT, lbl3_X_CT, lbl3_2_CT},
                { lbl4_1_CT, lbl4_X_CT, lbl4_2_CT},
                { lbl5_1_CT, lbl5_X_CT, lbl5_2_CT},
                { lbl6_1_CT, lbl6_X_CT, lbl6_2_CT},
                { lbl7_1_CT, lbl7_X_CT, lbl7_2_CT},
                { lbl8_1_CT, lbl8_X_CT, lbl8_2_CT}
            };

            if (waitTimer > 0 )
            {
                waitTimer -= 50;
            }
            else
            {
                waitTimer = 0;
            }

            ticketTimer += 50;
             

            lbl_queueSize.Text = lb_queue.Items.Count.ToString();
            int minutes = ((currentTimePerTicket * lb_queue.Items.Count) / 1000) / 60;
            int rest = ((currentTimePerTicket * lb_queue.Items.Count) / 1000) % 60;

            lbl_timeLeft.Text = minutes.ToString() + " : " + rest.ToString();

            switch (commit_state)
            {
                case commitEnum.idle:

                    break;


                case commitEnum.wait:
                    if (waitTimer == 0) {
                        commit_state = next_commit_state;
                    }
                    break;

                case commitEnum.activateNextCupon:
                    // aktivera nästa kupong
                    pt = new Point();
                    string[] coords = lbl_nextRow.Text.Split('.');
                    pt.X = int.Parse(coords[0]);
                    pt.Y = int.Parse(coords[1]);

                    SetCursorPos(pt.X, pt.Y);
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                    commit_state = commitEnum.wait;
                    waitTimer = 1000;
                    next_commit_state = commitEnum.fillInTickets;
             

                    break;
                case commitEnum.readInTickets:
                    string[] lines = System.IO.File.ReadAllLines("system.txt");
                    int count = System.IO.File.ReadAllLines("system.txt").Count();
                    lb_queue.Items.Clear();

                    // ta bort rubriken
                    for (int i = 1; i < count; i++)
                    {
                        lb_queue.Items.Add(lines[i]);

                    }

                    commit_state = commitEnum.activateNextCupon;
                    break;

                case commitEnum.fillInTickets:
                    if(lb_queue.Items.Count != 0) {
                        currentTimePerTicket = ticketTimer;
                        ticketTimer = 0;

                        lbl_current_ticket.Text = lb_queue.Items[0].ToString();
                        lb_queue.Items.RemoveAt(0);
                        currentRowToFill = 1;             // jump directly to second, skip E
                        commit_state = commitEnum.fillInTicket;
                    }
                    else
                    {

                    }

                    break;
                case commitEnum.fillInTicket:
                    if (currentRowToFill < 9)
                    {
                        commit_state = commitEnum.fillInRow;
                    }
                    else
                    {
                        waitTimer = 200;
                        commit_state = commitEnum.wait;
                        next_commit_state= commitEnum.verifyTicket;
                    }
                    break;

                case commitEnum.verifyTicket:
                    string[] ticketSigns = new string[10];

                    Label[,] SSlabels2 = new Label[,] {
                        { lbl1_1, lbl1_X, lbl1_2},
                        { lbl2_1, lbl2_X, lbl2_2},
                        { lbl3_1, lbl3_X, lbl3_2},
                        { lbl4_1, lbl4_X, lbl4_2},
                        { lbl5_1, lbl5_X, lbl5_2},
                        { lbl6_1, lbl6_X, lbl6_2},
                        { lbl7_1, lbl7_X, lbl7_2},
                        { lbl8_1, lbl8_X, lbl8_2}
                    };

                    for (int i = 0; i < 8; i++)
                    {
                        bool foundSign = false;
                        for (int j = 0; j < 3; j++)
                        {
                            for (int Xoffs = -2; Xoffs < 3; Xoffs++)
                            {
                                for (int YOffs = -2; YOffs < 3; YOffs++)
                                {
                                    Point pt = new Point();
                                    coords = SSlabels2[i, j].Text.Split('.');
                                    pt.X = int.Parse(coords[0]) + Xoffs;
                                    pt.Y = int.Parse(coords[1]) + YOffs;

                                    SetCursorPos(pt.X, pt.Y);

                                    IntPtr hdc2 = GetDC(IntPtr.Zero);
                                    uint pixel2 = GetPixel(hdc2, pt.X, pt.Y);
                                    ReleaseDC(IntPtr.Zero, hdc2);

                                    const int filledColor = 38130;

                                    if (pixel2 == filledColor)
                                    {
                                        ticketSigns[i] = signs[j];
                                        foundSign = true;
                                        break;
                                    }


                                }
                                if (foundSign)
                                    break;
                            }
                        }
                    }

                    bool correctFilledTicket = true;
                    string[] controlTicket = lbl_current_ticket.Text.Split(',');
                    for (int i = 0; i < 8; i++)
                    {
                        // control ticket has an letter first, therefore +1
                        if (controlTicket[i + 1].CompareTo(ticketSigns[i]) != 0)
                        {
                            correctFilledTicket = false;
                            break;
                        }
                    }

                    if (correctFilledTicket)
                    {
                        waitTimer = 2000;
                        commit_state = commitEnum.wait;
                        next_commit_state = commitEnum.expandPay;
                    }
                    else
                    {
                        commit_state = commitEnum.wait;
                        next_commit_state = commitEnum.wait;
                    }
                    break;
                case commitEnum.fillInRow:
                    Label[,] SSlabels = new Label[,] {
                        { lbl1_1, lbl1_X, lbl1_2},
                        { lbl2_1, lbl2_X, lbl2_2},
                        { lbl3_1, lbl3_X, lbl3_2},
                        { lbl4_1, lbl4_X, lbl4_2},
                        { lbl5_1, lbl5_X, lbl5_2},
                        { lbl6_1, lbl6_X, lbl6_2},
                        { lbl7_1, lbl7_X, lbl7_2},
                        { lbl8_1, lbl8_X, lbl8_2}
                    };

                    // fyll i rad
                    string[] ticket = lbl_current_ticket.Text.Split(',');
                    int sign;

                         
                    //fill ticket
                    if (ticket[currentRowToFill] == "1")
                        sign = 0;
                    else if (ticket[currentRowToFill] == "X")
                        sign = 1;
                    else
                        sign = 2;

                    coords = SSlabels[currentRowToFill-1, sign].Text.Split('.');
                    pt.X = int.Parse(coords[0]);
                    pt.Y = int.Parse(coords[1]);

                    SetCursorPos(pt.X, pt.Y);
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                    currentRowToFill++;

                    next_commit_state = commitEnum.fillInTicket;

                    waitTimer = 500;
                    commit_state = commitEnum.wait;
                    break;

                case commitEnum.expandPay:
                    // expandera betalinfo
                 /*   coords = lbl_review.Text.Split('.');
                    pt.X = int.Parse(coords[0]);
                    pt.Y = int.Parse(coords[1]);

                    SetCursorPos(pt.X, pt.Y);
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);*/
                    commit_state = commitEnum.wait;
                    waitTimer = 0;                          // TODO, ta bort kommentar och 1000 wait
                    next_commit_state = commitEnum.pay;
                    break;

                case commitEnum.pay:                    
                    // betala
                    coords = lbl_pay.Text.Split('.');
                    pt.X = int.Parse(coords[0]);
                    pt.Y = int.Parse(coords[1]);

                    SetCursorPos(pt.X, pt.Y);
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                    commit_state = commitEnum.wait;
                    waitTimer = 2000;
                    next_commit_state = commitEnum.confirm;
                    break;

                case commitEnum.confirm:
                    //bekräfta
                    coords = lbl_confirm.Text.Split('.');
                    pt.X = int.Parse(coords[0]);
                    pt.Y = int.Parse(coords[1]);

                    SetCursorPos(pt.X, pt.Y);
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                    commit_state = commitEnum.wait;
                    waitTimer = 2000;
                    next_commit_state = commitEnum.fillInTickets;  //TODO, ska vara after pay ack
                    break;

                case commitEnum.afterPayAck:
                    // ack after pay
                    coords = lbl_afterPay.Text.Split('.');
                    pt.X = int.Parse(coords[0]);
                    pt.Y = int.Parse(coords[1]);

                    SetCursorPos(pt.X, pt.Y);
                    mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);

                    commit_state = commitEnum.wait;
                    waitTimer = 2000;
                    next_commit_state = commitEnum.fillInTickets;

                    break;
                default:
                    break;    
            }

        
            pt = new Point();
            GetCursorPos(ref pt);

            label1.Text = pt.X.ToString() + "." + pt.Y.ToString();

            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, pt.X , pt.Y);

            ReleaseDC(IntPtr.Zero, hdc);

            label2.Text = pixel.ToString();

            uint noDigit = 12632256;


            // Check for space key
            if (lbl_afterPay.BackColor == Color.Yellow)
            {
                GetCursorPos(ref pt);
                lbl_afterPay.Text = pt.X.ToString() + "." + pt.Y.ToString();

                if (GetKeyState(0x01) > 0)
                {
                    lbl_afterPay.BackColor = Color.Green;
                }

            }
            else if (lbl_confirm.BackColor == Color.Yellow)
            {
                GetCursorPos(ref pt);
                lbl_confirm.Text = pt.X.ToString() + "." + pt.Y.ToString();

                if (GetKeyState(0x01) > 0)
                {
                    lbl_confirm.BackColor = Color.Green;
                }

            }
            else if (lbl_pay.BackColor == Color.Yellow)
            {
                GetCursorPos(ref pt);
                lbl_pay.Text = pt.X.ToString() + "." + pt.Y.ToString();

                if (GetKeyState(0x01) > 0)
                {
                    lbl_pay.BackColor = Color.Green;
                }

            }
            else if (lbl_review.BackColor == Color.Yellow)
            {
                GetCursorPos(ref pt);
                lbl_review.Text = pt.X.ToString() + "." + pt.Y.ToString();

                if (GetKeyState(0x01) > 0)
                {
                    lbl_review.BackColor = Color.Green;
                }

            }
            else if (lbl_nextRow.BackColor == Color.Yellow)
            {
                GetCursorPos(ref pt);
                lbl_nextRow.Text = pt.X.ToString() + "." + pt.Y.ToString();

                if (GetKeyState(0x01) > 0)
                {
                    lbl_nextRow.BackColor = Color.Green;
                }

            }
            else if (lbl_plus1.BackColor == Color.Yellow)
            {
                GetCursorPos(ref pt);
                lbl_plus1.Text = pt.X.ToString() + "." + pt.Y.ToString();

                if (GetKeyState(0x01) > 0)
                {
                    lbl_plus1.BackColor = Color.Green;
                }

            }
            else if (currSvenskaSpelX > -1 && currSvenskaSpelY > -1)
            {
                GetCursorPos(ref pt);

                labels[currSvenskaSpelX, currSvenskaSpelY].Text = pt.X.ToString() + "." + pt.Y.ToString();

                if (GetKeyState(0x01) > 0)
                {
                    labels[currSvenskaSpelX, currSvenskaSpelY].BackColor = Color.Green;

                    currSvenskaSpelX = -1;
                    currSvenskaSpelY = -1;

                }
            }
            else if (currCTX > -1 && currCTY > -1)
            {
                GetCursorPos(ref pt);

                labelsCT[currCTX, currCTY].Text = pt.X.ToString() + "." + pt.Y.ToString();

                if (GetKeyState(0x01) > 0)
                {
                    labelsCT[currCTX, currCTY].BackColor = Color.Green;

                    currCTX = -1;
                    currCTY = -1;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

            /*         uint MOUSEEVENTF_LEFTDOWN = 0x0002;
                     uint MOUSEEVENTF_LEFTUP = 0x0004;

                     SetCursorPos(3172,42);
                     GetCursorPos(ref pt);



                     mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);

                     */


          //  string[] textLines = System.IO.File.ReadAllLines(@"C:\slask\test.txt");


        }





        private void labelClicked(object sender, EventArgs e)
        {
            Label l;
            l = (Label)sender;

            Label[,] labels = new Label[,] {
                { lbl1_1, lbl1_X, lbl1_2},
                { lbl2_1, lbl2_X, lbl2_2},
                { lbl3_1, lbl3_X, lbl3_2},
                { lbl4_1, lbl4_X, lbl4_2},
                { lbl5_1, lbl5_X, lbl5_2},
                { lbl6_1, lbl6_X, lbl6_2},
                { lbl7_1, lbl7_X, lbl7_2},
                { lbl8_1, lbl8_X, lbl8_2}
            };

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (l.Name.CompareTo(labels[i, j].Name) ==  0)
                    {
                        currSvenskaSpelX = i;
                        currSvenskaSpelY = j;

                        labels[i, j].BackColor = Color.Yellow;
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void labelCT_Click(object sender, EventArgs e)
        {

            Label l;
            l = (Label)sender;

            Label[,] labels = new Label[,] {
                { lbl1_1_CT, lbl1_X_CT, lbl1_2_CT},
                { lbl2_1_CT, lbl2_X_CT, lbl2_2_CT},
                { lbl3_1_CT, lbl3_X_CT, lbl3_2_CT},
                { lbl4_1_CT, lbl4_X_CT, lbl4_2_CT},
                { lbl5_1_CT, lbl5_X_CT, lbl5_2_CT},
                { lbl6_1_CT, lbl6_X_CT, lbl6_2_CT},
                { lbl7_1_CT, lbl7_X_CT, lbl7_2_CT},
                { lbl8_1_CT, lbl8_X_CT, lbl8_2_CT}
            };

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (l.Name.CompareTo(labels[i, j].Name) == 0)
                    {
                        currCTX = i;
                        currCTY = j;

                        labels[i, j].BackColor = Color.Yellow;
                    }
                }
            }

        }

        private void lbl_plus1_Click(object sender, EventArgs e)
        {
            lbl_plus1.BackColor = Color.Yellow;
        }

        private void lbl_nextRow_Click(object sender, EventArgs e)
        {
            lbl_nextRow.BackColor = Color.Yellow;
        }

        private void lbl_review_Click(object sender, EventArgs e)
        {
            lbl_review.BackColor = Color.Yellow;
        }

        private void lbl_pay_Click(object sender, EventArgs e)
        {
            lbl_pay.BackColor = Color.Yellow;
        }

        private void btn_run_Click(object sender, EventArgs e)
        {
            commit_state = commitEnum.readInTickets;
                /*//fasdfasd
            Label[,] CTlabels = new Label[,] {
                { lbl1_1_CT, lbl1_X_CT, lbl1_2_CT},
                { lbl2_1_CT, lbl2_X_CT, lbl2_2_CT},
                { lbl3_1_CT, lbl3_X_CT, lbl3_2_CT},
                { lbl4_1_CT, lbl4_X_CT, lbl4_2_CT},
                { lbl5_1_CT, lbl5_X_CT, lbl5_2_CT},
                { lbl6_1_CT, lbl6_X_CT, lbl6_2_CT},
                { lbl7_1_CT, lbl7_X_CT, lbl7_2_CT},
                { lbl8_1_CT, lbl8_X_CT, lbl8_2_CT}
            };

            Label[,] STlabels = new Label[,] {
                { lbl1_1, lbl1_X, lbl1_2},
                { lbl2_1, lbl2_X, lbl2_2},
                { lbl3_1, lbl3_X, lbl3_2},
                { lbl4_1, lbl4_X, lbl4_2},
                { lbl5_1, lbl5_X, lbl5_2},
                { lbl6_1, lbl6_X, lbl6_2},
                { lbl7_1, lbl7_X, lbl7_2},
                { lbl8_1, lbl8_X, lbl8_2}
            };



            // gå igenom raderna och kolumnerna, om hittat tecken, för över det till ticket
            uint noDigit = 12632256;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    bool marked = false;
                    
                    for  (int xOffs = -2; xOffs < 2; xOffs++)
                    {
                        for (int yOffs = -2; yOffs < 2; yOffs++)
                        {
                            string[] savedCoord = CTlabels[i, j].Text.Split('.');
                            
                            pt = new Point();
                            pt.X = int.Parse(savedCoord[0]) + xOffs;
                            pt.Y = int.Parse(savedCoord[1]) + yOffs;

                            IntPtr hdc = GetDC(IntPtr.Zero);
                            uint pixel = GetPixel(hdc, pt.X, pt.Y);
                            ReleaseDC(IntPtr.Zero, hdc);
                            
                            SetCursorPos(pt.X, pt.Y);

                            if (pixel != noDigit)
                            {
                                string[] savedCoord2 = STlabels[i, j].Text.Split('.');

                                pt.X = int.Parse(savedCoord2[0]) + xOffs;
                                pt.Y = int.Parse(savedCoord2[1]) + yOffs;
                                SetCursorPos(pt.X, pt.Y);
                                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                                marked = true;
                            }

                            System.Threading.Thread.Sleep(100);
                            if (marked)
                                break;
                        }

                        if (marked)
                            break;
                    }

                }
            }*/

        }

        private void save_Click(object sender, EventArgs e)
        {
            Label[,] CTlabels = new Label[,] {
                { lbl1_1_CT, lbl1_X_CT, lbl1_2_CT},
                { lbl2_1_CT, lbl2_X_CT, lbl2_2_CT},
                { lbl3_1_CT, lbl3_X_CT, lbl3_2_CT},
                { lbl4_1_CT, lbl4_X_CT, lbl4_2_CT},
                { lbl5_1_CT, lbl5_X_CT, lbl5_2_CT},
                { lbl6_1_CT, lbl6_X_CT, lbl6_2_CT},
                { lbl7_1_CT, lbl7_X_CT, lbl7_2_CT},
                { lbl8_1_CT, lbl8_X_CT, lbl8_2_CT}
            };

            Label[,] STlabels = new Label[,] {
                { lbl1_1, lbl1_X, lbl1_2},
                { lbl2_1, lbl2_X, lbl2_2},
                { lbl3_1, lbl3_X, lbl3_2},
                { lbl4_1, lbl4_X, lbl4_2},
                { lbl5_1, lbl5_X, lbl5_2},
                { lbl6_1, lbl6_X, lbl6_2},
                { lbl7_1, lbl7_X, lbl7_2},
                { lbl8_1, lbl8_X, lbl8_2}
            };


            string[] lines = new string[21];

            for (int i = 0; i < 8; i++)
            {
                lines[i] = STlabels[i, 0].Text + ',' + STlabels[i, 1].Text + ',' + STlabels[i, 2].Text;
            }

            for (int i = 0; i < 8; i++)
            {
                lines[8 + i] = CTlabels[i, 0].Text + ',' + CTlabels[i, 1].Text + ',' + CTlabels[i, 2].Text;
            }

            lines[16] = lbl_nextRow.Text;
            lines[17] = lbl_review.Text;
            lines[18] = lbl_pay.Text;
            lines[19] = lbl_confirm.Text;
            lines[20] = lbl_plus1.Text;
            



            System.IO.File.WriteAllLines(@"C:\slask\test.txt", lines);


     //       System.IO.File.OpenWrite("@"C:\slask\test.txt");

            //  string[] textLines = System.IO.File.ReadAllLines(@"C:\slask\test.txt");
        }

        private void btn_load_Click(object sender, EventArgs e)
        {



            Label[,] CTlabels = new Label[,] {
                { lbl1_1_CT, lbl1_X_CT, lbl1_2_CT},
                { lbl2_1_CT, lbl2_X_CT, lbl2_2_CT},
                { lbl3_1_CT, lbl3_X_CT, lbl3_2_CT},
                { lbl4_1_CT, lbl4_X_CT, lbl4_2_CT},
                { lbl5_1_CT, lbl5_X_CT, lbl5_2_CT},
                { lbl6_1_CT, lbl6_X_CT, lbl6_2_CT},
                { lbl7_1_CT, lbl7_X_CT, lbl7_2_CT},
                { lbl8_1_CT, lbl8_X_CT, lbl8_2_CT}
            };

            Label[,] STlabels = new Label[,] {
                { lbl1_1, lbl1_X, lbl1_2},
                { lbl2_1, lbl2_X, lbl2_2},
                { lbl3_1, lbl3_X, lbl3_2},
                { lbl4_1, lbl4_X, lbl4_2},
                { lbl5_1, lbl5_X, lbl5_2},
                { lbl6_1, lbl6_X, lbl6_2},
                { lbl7_1, lbl7_X, lbl7_2},
                { lbl8_1, lbl8_X, lbl8_2}
            };


            string[] lines = System.IO.File.ReadAllLines(@"C:\slask\test.txt");

            for (int i = 0; i < 8; i++)
            {
                string[] linecolumn = lines[i].Split(',');
                STlabels[i, 0].Text = linecolumn[0];
                STlabels[i, 1].Text = linecolumn[1];
                STlabels[i, 2].Text = linecolumn[2];
            }

            for (int i = 0; i < 8; i++)
            { 
                string[] linecolumn = lines[8+i].Split(',');
                CTlabels[i, 0].Text = linecolumn[0];
                CTlabels[i, 1].Text = linecolumn[1];
                CTlabels[i, 2].Text = linecolumn[2];
            }

            lbl_nextRow.Text = lines[16];
            lbl_review.Text = lines[17];
            lbl_pay.Text = lines[18];
            lbl_confirm.Text = lines[19];
            lbl_plus1.Text = lines[20];
        }

        private void btn_runAll_Click(object sender, EventArgs e)
        {
          /*  int cupongs = 0;

            while (cupongs < int.Parse(tb_cupons.Text))
            {
     



                

             


                cupongs++;

                //stega fram nästa kupong
                coords = lbl_plus1.Text.Split('.');
                pt.X = int.Parse(coords[0]);
                pt.Y = int.Parse(coords[1]);

                SetCursorPos(pt.X, pt.Y);
                System.Threading.Thread.Sleep(500);

                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                System.Threading.Thread.Sleep(500);
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                System.Threading.Thread.Sleep(500);

                // vänta ut textbox

                System.Threading.Thread.Sleep(15000);

                // space klicked
                if (GetKeyState(0x20) > 0)
                {
                    //avbryt
                    break;

                }
            }*/
        }

        private void btn_runFile_Click(object sender, EventArgs e)
        {
            /*     string[] coords = lbl_plus1.Text.Split('.');
                 pt.X = int.Parse(coords[0]);
                 pt.Y = int.Parse(coords[1]);

                 SetCursorPos(pt.X, pt.Y);
                 System.Threading.Thread.Sleep(500);

                 mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);



                 Label[,] STlabels = new Label[,] {
                     { lbl1_1, lbl1_X, lbl1_2},
                     { lbl2_1, lbl2_X, lbl2_2},
                     { lbl3_1, lbl3_X, lbl3_2},
                     { lbl4_1, lbl4_X, lbl4_2},
                     { lbl5_1, lbl5_X, lbl5_2},
                     { lbl6_1, lbl6_X, lbl6_2},
                     { lbl7_1, lbl7_X, lbl7_2},
                     { lbl8_1, lbl8_X, lbl8_2}
                 };

                 //first line is Stryktipset, first column is E
                 Point pt = new Point();

                 for (int i = 1; i < count; i++)
                 {

                     // fyll i rad
                     string[] ticket = lines[i].Split(',');
                     int sign;
                     for (int j = 1; j < 9; j++) {
                         //fill ticket
                         if (ticket[j] == "1")
                             sign = 0;
                         else if (ticket[j] == "X")
                             sign = 1;
                         else
                             sign = 2;


                         coords = STlabels[j-1,sign].Text.Split('.');
                         pt.X = int.Parse(coords[0]);
                         pt.Y = int.Parse(coords[1]);

                         SetCursorPos(pt.X, pt.Y);
                         mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                         System.Threading.Thread.Sleep(500);
                     }

                     System.Threading.Thread.Sleep(1000);

                     // expandera betalinfo
                     coords = lbl_review.Text.Split('.');
                     pt.X = int.Parse(coords[0]);
                     pt.Y = int.Parse(coords[1]);

                     SetCursorPos(pt.X, pt.Y);
                     mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                     System.Threading.Thread.Sleep(1000);

                     // betala
                     coords = lbl_pay.Text.Split('.');
                     pt.X = int.Parse(coords[0]);
                     pt.Y = int.Parse(coords[1]);

                     SetCursorPos(pt.X, pt.Y);
                     mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                     System.Threading.Thread.Sleep(1000);

                     //bekräfta
                     coords = lbl_confirm.Text.Split('.');
                     pt.X = int.Parse(coords[0]);
                     pt.Y = int.Parse(coords[1]);

                     SetCursorPos(pt.X, pt.Y);
                     mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                     System.Threading.Thread.Sleep(2000);

                     // bekräfta efter betalningsrutan
                     coords = lbl_afterPay.Text.Split('.');
                     pt.X = int.Parse(coords[0]);
                     pt.Y = int.Parse(coords[1]);

                     SetCursorPos(pt.X, pt.Y);
                     mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)pt.X, (uint)pt.Y, 0, (UIntPtr)0);
                     System.Threading.Thread.Sleep(2000);
                 }*/
        }

        private void lbl_confirm_Click(object sender, EventArgs e)
        {
            lbl_confirm.BackColor = Color.Yellow;
        }

        private void label31_Click(object sender, EventArgs e)
        {

        }

        private void lbl_afterPay_Click(object sender, EventArgs e)
        {
            lbl_afterPay.BackColor = Color.Yellow;
        }
    }
}    



