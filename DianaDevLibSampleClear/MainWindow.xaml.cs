using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Windows.Input;

using static DianaDevLibSample.DianaDevLibDLL;
using static DianaDevLibSample.Question;

namespace DianaDevLibSample
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PlotDataConnector pdc;
        private MainViewModel VM;
        private IntPtr pDiana = IntPtr.Zero;
        private DBSender dbsender = new DBSender();
        private StreamReader reader;
        private IList<Testee> testeesList;
        private IList<Gender> gendersList;
        public class ComboBoxDeviceItem
        {
            public string Text { get; set; }
            public DeviceInfo Value { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }


        void DataReceivedCallback(System.UInt32 dwUser, [MarshalAs(UnmanagedType.LPArray, SizeConst = 8)] System.UInt16[] pDataPacket)
        {
            Dispatcher.InvokeAsync(() => UpdateDataUI(pDataPacket));
        }

        void ConnectionChangedCallback(System.UInt32 dwUser, System.UInt32 dwChangeType)
        {
            Dispatcher.InvokeAsync(() => UpdateDeviceList());
        }


        void DianaInfoCallback(System.UInt32 dwUser, [MarshalAs(UnmanagedType.LPStr)] string lpstrDianaInfo)
        {
            Dispatcher.InvokeAsync(() => UpdateDianaInfo(lpstrDianaInfo));
        }

        void DispChangedCallback(System.UInt32 dwUser, UInt16 wChannelIndex, UInt16 wValue)
        {
            Dispatcher.InvokeAsync(() => UpdateChannelDisp(wChannelIndex, wValue));
        }

        void AmplChangedCallback(System.UInt32 dwUser, UInt16 wChannelIndex, Byte bValue)
        {
            Dispatcher.InvokeAsync(() => UpdateChannelAmpl(wChannelIndex, bValue));
        }

        void OptionalTypeChangedCallback(System.UInt32 dwUser, Byte bValue)
        {
            Dispatcher.InvokeAsync(() => UpdateOptionalType(bValue));
        }

        private void UpdateOptionalType(byte bValue)
        {
            Dispatcher.InvokeAsync(() => UpdateOptionalTypeGUI(bValue));
        }

        private DataReceivedCallback OnDataReceived;
        private ConnectionChangedCallback OnConnectionChanged;
        private DianaInfoCallback OnDianaInfo;
        private DispChangedCallback OnDispChanged;
        private AmplChangedCallback OnAmplChanged;
        private OptionalTypeChangedCallback OnOptionalTypeChanged;

        public MainWindow()
        {
            InitializeComponent();
            pdc = PlotDataConnector.getInstance();
            VM = new MainViewModel();
            this.DataContext = VM;
        }

        const uint S_OK = 0;
        const uint E_ACCESSDENIED = 0x80070005;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            uint res = Init();
            switch (Init())
            {
                case S_OK:

                    break;
                case E_ACCESSDENIED:
                    MessageBox.Show("Отсутствуют права на использование: нужен USB-ключ", "Ошибка",   MessageBoxButton.OK, MessageBoxImage.Error);

                    return;
                default:
                    MessageBox.Show("Непредвиденная ошибка", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
            }
            CreateDiana(out pDiana);
            if (pDiana == IntPtr.Zero)
                return;

            OnDataReceived = DataReceivedCallback;
            SetDataReceivedCallback(pDiana, 0, OnDataReceived);

            OnConnectionChanged = ConnectionChangedCallback;
            SetConnectionChangedCallback(pDiana, 0, OnConnectionChanged);

            OnDianaInfo = DianaInfoCallback;
            SetDianaInfoCallback(pDiana, 0, OnDianaInfo);

            OnDispChanged = DispChangedCallback;
            SetDispChangedCallback(pDiana, 0, OnDispChanged);

            OnAmplChanged = AmplChangedCallback;
            SetAmplChangedCallback(pDiana, 0, OnAmplChanged);

            OnOptionalTypeChanged = OptionalTypeChangedCallback;
            SetOptionalTypeChangedCallback(pDiana, 0, OnOptionalTypeChanged);

            UpdateDeviceList();


        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (pDiana != IntPtr.Zero) {
                FreeDiana(pDiana);
                pDiana = IntPtr.Zero;
            }
            Free();
        }

        private void UpdateGUI()
        {
            UpdateDispGUI();
            UpdateAmplGUI();
            UpdateOptionalTypeGUI(GetOptionalType(pDiana));
            UpdateTestModeGUI(GetTestMode(pDiana));
        }

        private void UpdateOptionalTypeGUI(Byte value)
        {
            switch (value)
            {
                case OT_TYPE_1:
                    tbOptionalType.Text = "Тип Доп: OT_TYPE_1";
                    break;
                case OT_TYPE_2:
                    tbOptionalType.Text = "Тип Доп: OT_TYPE_2";
                    break;
            }

        }


        private void UpdateTestModeGUI(bool value)
        {
            tbTestMode.Text = value ? "Тестовый режим включен" : "Тестовый режим выключен";
        }

        private void UpdateDispGUI()
        {
            tbEDADisp.Text = GetDisp(pDiana, CH_EDA_INDEX).ToString();
            tbOptionalDisp.Text = GetDisp(pDiana, CH_OPTIONAL_INDEX).ToString();
            tbTEDADisp.Text = GetDisp(pDiana, CH_TEDA_INDEX).ToString();
            tbBVDisp.Text = GetDisp(pDiana, CH_BV_INDEX).ToString();
            tbTRDisp.Text = GetDisp(pDiana, CH_TR_INDEX).ToString();
            tbARDisp.Text = GetDisp(pDiana, CH_AR_INDEX).ToString();
            tbPLEDisp.Text = GetDisp(pDiana, CH_PLE_INDEX).ToString();
            tbTremorDisp.Text = GetDisp(pDiana, CH_TREMOR_INDEX).ToString();
        }

        private void UpdateAmplGUI()
        {
            tbEDAAmpl.Text = GetAmpl(pDiana, CH_EDA_INDEX).ToString();
            tbOptionalAmpl.Text = GetAmpl(pDiana, CH_OPTIONAL_INDEX).ToString();
            tbTEDAAmpl.Text = GetAmpl(pDiana, CH_TEDA_INDEX).ToString();
            tbBVAmpl.Text = GetAmpl(pDiana, CH_BV_INDEX).ToString();
            tbTRAmpl.Text = GetAmpl(pDiana, CH_TR_INDEX).ToString();
            tbARAmpl.Text = GetAmpl(pDiana, CH_AR_INDEX).ToString();
            tbPLEAmpl.Text = GetAmpl(pDiana, CH_PLE_INDEX).ToString();
            tbTremorAmpl.Text = GetAmpl(pDiana, CH_TREMOR_INDEX).ToString();
        }

        private void UpdateDeviceList()
        {
            cbDeviceList.Items.Clear();
            if (!GetDevCount(pDiana, out System.UInt32 count))
                return;
            DeviceInfo[] pDevInfo = new DeviceInfo[count];
            if (!GetDevList(pDiana, pDevInfo, ref count))
                return;
  
            foreach (var di in pDevInfo)
            {
                string sn = new string(di.SerialNumber);
                ComboBoxDeviceItem item = new ComboBoxDeviceItem
                {
                    Text = sn.Substring(0, sn.IndexOf('\0')), //перевод из null terminated string
                    Value = di
                };

                cbDeviceList.Items.Add(item);
            }
            if (cbDeviceList.Items.Count > 0)
                cbDeviceList.SelectedIndex = 0;
        }

        private void UpdateDataUI(System.UInt16[] pDataPacket)
        {
            Task.Run(() => dbsender.SendData(pDataPacket));
            pdc.DATA_PACKAGE = pDataPacket;
            tbOptional.Text = pDataPacket[CH_OPTIONAL_INDEX].ToString();
            tbEDA.Text = pDataPacket[CH_EDA_INDEX].ToString();
            tbTR.Text = pDataPacket[CH_TR_INDEX].ToString();
            tbAR.Text = pDataPacket[CH_AR_INDEX].ToString();
            tbPLE.Text = pDataPacket[CH_PLE_INDEX].ToString();
            tbTremor.Text = pDataPacket[CH_TREMOR_INDEX].ToString();
            tbBV.Text = pDataPacket[CH_BV_INDEX].ToString();
            tbTEDA.Text = pDataPacket[CH_TEDA_INDEX].ToString();
            tbAbsBV.Text = pDataPacket[CH_ABSBV_INDEX].ToString();
        }

        private void UpdateDianaInfo(string lpstrDianaInfo)
        {
            tbDianaInfo.Text = lpstrDianaInfo;
        }

        private void UpdateChannelDisp(UInt16 wChannelIndex, UInt16 wValue)
        {
            TextBox tb = null;
            switch (wChannelIndex)
            {
                case 0: tb = tbOptionalDisp; break;
                case 1: tb = tbEDADisp; break;
                case 2: tb = tbTRDisp; break;
                case 3: tb = tbARDisp; break;
                case 4: tb = tbPLEDisp; break;
                case 5: tb = tbTremorDisp; break;
                case 6: tb = tbBVDisp; break;
                case 7: tb = tbTEDADisp; break;
            }
            if (tb != null)
                tb.Text = wValue.ToString();
        }

        private void UpdateChannelAmpl(UInt16 wChannelIndex, Byte wValue)
        {
            TextBox tb = null;
            switch (wChannelIndex)
            {
                case 0: tb = tbOptionalAmpl; break;
                case 1: tb = tbEDAAmpl; break;
                case 2: tb = tbTRAmpl; break;
                case 3: tb = tbARAmpl; break;
                case 4: tb = tbPLEAmpl; break;
                case 5: tb = tbTremorAmpl; break;
                case 6: tb = tbBVAmpl; break;
                case 7: tb = tbTEDAAmpl; break;
            }
            if (tb != null)
                tb.Text = wValue.ToString();
        }

        private void Ampl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Ampl_TextChangeFinish((TextBox)sender);
        }

        private void Ampl_TextChangeFinish(TextBox textBox)
        {
            UInt16 chIndex = UInt16.Parse((string)textBox.Tag);
            Byte needValue;
            if (Byte.TryParse(textBox.Text, out needValue))
            {
                Byte needValueByte = (Byte)(needValue > 255 ? 255 : needValue);
                textBox.Text = needValueByte.ToString();
                SendAmpl(pDiana, chIndex, needValueByte);
            }
        }
        private static bool IsTextAllowed(string text)
        {
            Regex regex = new Regex("[^0-9]");
            return !regex.IsMatch(text);
        }

        private void AmplMinus_Button_Click(object sender, RoutedEventArgs e)
        {
            UInt16 chIndex = UInt16.Parse((string)((Control)sender).Tag);
            Byte curValue = GetAmpl(pDiana, chIndex);
            if (curValue > 0)
                SendAmpl(pDiana, chIndex, (Byte)(curValue - 1));
        }

        private void AmplPlus_Button_Click(object sender, RoutedEventArgs e)
        {
            UInt16 chIndex = UInt16.Parse((string)((Control)sender).Tag);
            Byte curValue = GetAmpl(pDiana, chIndex);
            if (curValue < 255)
                SendAmpl(pDiana, chIndex, (Byte)(curValue + 1));
        }

        private void DispPlus_Button_Click(object sender, RoutedEventArgs e)
        {
            UInt16 chIndex = UInt16.Parse((string)((Control)sender).Tag);
            UInt16 curValue = GetDisp(pDiana, chIndex);
            if (curValue < 4095)
                SendDisp(pDiana, chIndex, (UInt16)(curValue + 1));
        }

        private void PreviewDispTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void DispMinus_Button_Click(object sender, RoutedEventArgs e)
        {
            UInt16 chIndex = UInt16.Parse((string)((Control)sender).Tag);
            UInt16 curValue = GetDisp(pDiana, chIndex);
            if (curValue > 0)
                SendDisp(pDiana, chIndex, (UInt16)(curValue - 1));
        }

        private void Disp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                Disp_TextChangeFinish((TextBox)sender);
        }

        private void Disp_TextChangeFinish(TextBox textBox)
        {
            UInt16 chIndex = UInt16.Parse((string)textBox.Tag);
            UInt16 needValue;
            if (UInt16.TryParse(textBox.Text, out needValue))
            {
                UInt16 needValueShort = (UInt16)(needValue > 4095 ? 4095 : needValue);
                textBox.Text = needValueShort.ToString();
                SendDisp(pDiana, chIndex, needValueShort);

            }
        }

        private void PreviewAmplTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text);
        }

        private void Optional_Button_Click(object sender, RoutedEventArgs e)
        {
            if (GetOptionalType(pDiana) == OT_TYPE_1)
                SendOptionalType(pDiana, OT_TYPE_2);
            else
                SendOptionalType(pDiana, OT_TYPE_1);
        }

        private void cbDeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbDianaInfo.Text = "";
            ComboBox cmb = (ComboBox)sender;
            if (cmb.SelectedItem != null)
            {
                DeviceInfo devInfo = ((ComboBoxDeviceItem)cmb.SelectedItem).Value;
                if (OpenDevice(pDiana, ref devInfo))
                {
                    RequestDianaInfo(pDiana);
                    UpdateGUI();
                }
            }
            else
            {
                CloseDevice(pDiana);
            }
        }

        private void TestMode_Button_Click(object sender, RoutedEventArgs e)
        {
            SetTestMode(pDiana, !GetTestMode(pDiana));
            UpdateTestModeGUI(GetTestMode(pDiana));
        }

        //----------ИСПЫТУЕМЫЕ----------//

        private void AddTestee(object sender, RoutedEventArgs e)
        {
            Testee temp = new Testee
            {
                Surname = tbSurname.Text,
                Name = tbName.Text,
                Patronium = tbPatronium.Text,
                BirthDate = (DateTime)dpBirthDate.SelectedDate,
                ID_gender = (int)cbGender.SelectedValue
            };
            dbsender.NewTestee(temp);
            testeesList = RefreshTesteesList();
        }

        private IList<Testee> RefreshTesteesList()
        {
            dbsender.PopulateTesteesList();
            return dbsender.GetTesteesList();
        }
        private IList<Gender> RefreshGendersList()
        {
            dbsender.PopulateGendersList();
            return dbsender.GetGendersList();
        }

        //----------ГРАФОНИЙ----------//
        private void RunTest(object sender, RoutedEventArgs e)
        {
            if(cbMethodsList.SelectedIndex != -1 && cbTesteeList.SelectedIndex != -1)
            {
                dbsender.CurrentTestee = (int)cbTesteeList.SelectedValue;
                ComboBoxItem SelectedMethod = (ComboBoxItem)cbMethodsList.SelectedItem;
                switch(SelectedMethod.Content)
                {
                    case "Методика Бойко":
                        reader = new StreamReader(@"Questions\\questionsBoyko.txt", Encoding.GetEncoding("windows-1251"));
                        break;

                    default:
                        reader = new StreamReader(@"Questions\\questionsBoyko.txt", Encoding.GetEncoding("windows-1251"));
                        break;
                }  
                btnTestRun.IsEnabled = false;
                cbMethodsList.IsEnabled = false;
                cbTesteeList.IsEnabled = false;
                grdQuestion.IsEnabled = true;

                tbQuestion.Text = reader.ReadLine();
            }
            else if (cbMethodsList.SelectedIndex == -1) { MessageBox.Show("Не выбрана методика тестирования", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            else if (cbTesteeList.SelectedIndex == -1) { MessageBox.Show("Не выбран испытуемый", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void btnHandleAnswer(object sender, RoutedEventArgs e)
        {
            string inputLine;
            Question temp;
            Button btn = (Button)sender;

            switch (btn.Content)
            {
                case "1":
                    temp = new Question(tbQuestion.Text, 1, DateTime.Now);
                    break;

                case "2":
                    temp = new Question(tbQuestion.Text, 2, DateTime.Now);
                    break;

                case "3":
                    temp = new Question(tbQuestion.Text, 3, DateTime.Now);
                    break;

                case "4":
                    temp = new Question(tbQuestion.Text, 4, DateTime.Now);
                    break;

                case "5":
                    temp = new Question(tbQuestion.Text, 5, DateTime.Now);
                    break;

                case "6":
                    temp = new Question(tbQuestion.Text, 6, DateTime.Now);
                    break;

                case "7":
                    temp = new Question(tbQuestion.Text, 7, DateTime.Now);
                    break;

                default:
                    temp = new Question(tbQuestion.Text, 0, DateTime.Now);
                    break;
            }
            Task.Run(() => dbsender.SendQuestion(temp));
            if ((inputLine = reader.ReadLine()) != null)
            {
                tbQuestion.Text = inputLine;
            }
            else
            {
                tbQuestion.Text = "Тест окончен.";
                btnTestRun.IsEnabled = true;
                cbMethodsList.IsEnabled = true;
                cbTesteeList.IsEnabled = true;
                grdQuestion.IsEnabled = false;

                dbsender.CurrentTestee = -1;
                cbMethodsList.SelectedIndex = -1;
                cbTesteeList.SelectedIndex = -1;
            }
                
        }

        //----------НАСТРОЙКИ----------//

        private void MinMax_Button_Click(object sender, RoutedEventArgs e)
        {   
            TextBox tb;
            Button btn = (Button)sender;
            switch(btn.Name.Substring(3, btn.Name.IndexOf("M") - 3))
            {
                case "Optional":
                    if (btn.Name.Contains("MIN"))
                        tb = tbOptionalMin;
                    else tb = tbOptionalMax;
                    break;
                case "EDA":
                    if (btn.Name.Contains("MIN"))
                        tb = tbEDAMin;
                    else tb = tbEDAMax;
                    break;
                case "TR":
                    if (btn.Name.Contains("MIN"))
                        tb = tbTRMin;
                    else tb = tbTRMax;
                    break;
                case "AR":
                    if (btn.Name.Contains("MIN"))
                        tb = tbARMin;
                    else tb = tbARMax;
                    break;
                case "PLE":
                    if (btn.Name.Contains("MIN"))
                        tb = tbPLEMin;
                    else tb = tbPLEMax;
                    break;
                case "TRE":
                    if (btn.Name.Contains("MIN"))
                        tb = tbTREMin;
                    else tb = tbTREMax;
                    break;
                case "BV":
                    if (btn.Name.Contains("MIN"))
                        tb = tbBVMin;
                    else tb = tbBVMax;
                    break;
                case "TEDA":
                    if (btn.Name.Contains("MIN"))
                        tb = tbTEDAMin;
                    else tb = tbTEDAMax;
                    break;
                case "ABSBV":
                    if (btn.Name.Contains("MIN"))
                        tb = tbABSBVMin;
                    else tb = tbABSBVMax;
                    break;
                default:
                    tb = tbOptionalMin;
                    break;
            }
            UInt16 tbValue = Convert.ToUInt16(tb.Text);
            if (btn.Name.Contains("Minus") && (tbValue > 0)) tbValue--;
            else if (btn.Name.Contains("Plus") && (tbValue < 16000)) tbValue++;
            tb.Text = tbValue.ToString();

        }

        private void handleTabControl(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl) //if this event fired from TabControl then enter
            {
                if (tiPrefs != null)
                {
                    if (tiPrefs.IsSelected)
                    {
                        cbOptionalAllow.IsChecked = VM.Prefs[0]._isAvailable;
                        tbOptionalMin.Text = VM.Prefs[0]._minValue.ToString();
                        tbOptionalMax.Text = VM.Prefs[0]._maxValue.ToString();

                        cbEDAAllow.IsChecked = VM.Prefs[1]._isAvailable;
                        tbEDAMin.Text = VM.Prefs[1]._minValue.ToString();
                        tbEDAMax.Text = VM.Prefs[1]._maxValue.ToString();

                        cbTRAllow.IsChecked = VM.Prefs[2]._isAvailable;
                        tbTRMin.Text = VM.Prefs[2]._minValue.ToString();
                        tbTRMax.Text = VM.Prefs[2]._maxValue.ToString();

                        cbARAllow.IsChecked = VM.Prefs[3]._isAvailable;
                        tbARMin.Text = VM.Prefs[3]._minValue.ToString();
                        tbARMax.Text = VM.Prefs[3]._maxValue.ToString();

                        cbPLEAllow.IsChecked = VM.Prefs[4]._isAvailable;
                        tbPLEMin.Text = VM.Prefs[4]._minValue.ToString();
                        tbPLEMax.Text = VM.Prefs[4]._maxValue.ToString();

                        cbTREAllow.IsChecked = VM.Prefs[5]._isAvailable;
                        tbTREMin.Text = VM.Prefs[5]._minValue.ToString();
                        tbTREMax.Text = VM.Prefs[5]._maxValue.ToString();

                        cbBVAllow.IsChecked = VM.Prefs[6]._isAvailable;
                        tbBVMin.Text = VM.Prefs[6]._minValue.ToString();
                        tbBVMax.Text = VM.Prefs[6]._maxValue.ToString();

                        cbTEDAAllow.IsChecked = VM.Prefs[7]._isAvailable;
                        tbTEDAMin.Text = VM.Prefs[7]._minValue.ToString();
                        tbTEDAMax.Text = VM.Prefs[7]._maxValue.ToString();

                        cbABSBVAllow.IsChecked = VM.Prefs[8]._isAvailable;
                        tbABSBVMin.Text = VM.Prefs[8]._minValue.ToString();
                        tbABSBVMax.Text = VM.Prefs[8]._maxValue.ToString();
                    }
                    if(tiDisplay.IsSelected)
                    {
                        testeesList = RefreshTesteesList();
                        cbTesteeList.ItemsSource = testeesList;
                        cbTesteeList.DisplayMemberPath = "FullName";
                        cbTesteeList.SelectedValuePath = "ID_testee";
                    }
                    if (tiTestee.IsSelected)
                    {
                        testeesList = RefreshTesteesList();
                        gendersList = RefreshGendersList();

                        cbTesteeListEdit.ItemsSource = testeesList;
                        cbTesteeListEdit.DisplayMemberPath = "FullName";
                        cbTesteeListEdit.SelectedValuePath = "ID_testee";

                        cbGender.ItemsSource = gendersList;
                        cbGender.DisplayMemberPath = "Description";
                        cbGender.SelectedValuePath = "ID_gender";
                    }
                }
            }
        }

        private void applyPrefs(object sender, RoutedEventArgs e)
        {
            PlotModelParameter[] temp = new PlotModelParameter[9];

            temp[0]._isAvailable = cbOptionalAllow.IsChecked.GetValueOrDefault();
            temp[0]._minValue = Convert.ToUInt16(tbOptionalMin.Text);
            temp[0]._maxValue = Convert.ToUInt16(tbOptionalMax.Text);

            temp[1]._isAvailable = cbEDAAllow.IsChecked.GetValueOrDefault();
            temp[1]._minValue = Convert.ToUInt16(tbEDAMin.Text);
            temp[1]._maxValue = Convert.ToUInt16(tbEDAMax.Text);

            temp[2]._isAvailable = cbTRAllow.IsChecked.GetValueOrDefault();
            temp[2]._minValue = Convert.ToUInt16(tbTRMin.Text);
            temp[2]._maxValue = Convert.ToUInt16(tbTRMax.Text);

            temp[3]._isAvailable = cbARAllow.IsChecked.GetValueOrDefault();
            temp[3]._minValue = Convert.ToUInt16(tbARMin.Text);
            temp[3]._maxValue = Convert.ToUInt16(tbARMax.Text);

            temp[4]._isAvailable = cbPLEAllow.IsChecked.GetValueOrDefault();
            temp[4]._minValue = Convert.ToUInt16(tbPLEMin.Text);
            temp[4]._maxValue = Convert.ToUInt16(tbPLEMax.Text);

            temp[5]._isAvailable = cbTREAllow.IsChecked.GetValueOrDefault();
            temp[5]._minValue = Convert.ToUInt16(tbTREMin.Text);
            temp[5]._maxValue = Convert.ToUInt16(tbTREMax.Text);

            temp[6]._isAvailable = cbBVAllow.IsChecked.GetValueOrDefault();
            temp[6]._minValue = Convert.ToUInt16(tbBVMin.Text);
            temp[6]._maxValue = Convert.ToUInt16(tbBVMax.Text);

            temp[7]._isAvailable = cbTEDAAllow.IsChecked.GetValueOrDefault();
            temp[7]._minValue = Convert.ToUInt16(tbTEDAMin.Text);
            temp[7]._maxValue = Convert.ToUInt16(tbTEDAMax.Text);

            temp[8]._isAvailable = cbABSBVAllow.IsChecked.GetValueOrDefault();
            temp[8]._minValue = Convert.ToUInt16(tbABSBVMin.Text);
            temp[8]._maxValue = Convert.ToUInt16(tbABSBVMax.Text);

            if (PrefsIsValidated(temp)) { VM.updatePrefs(temp); SetVisibility(temp); }
            else { MessageBox.Show("Заданы некорректные параметры.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void SetVisibility(PlotModelParameter[] temp)
        {
            if (temp[0]._isAvailable) { plot1.Visibility = Visibility.Visible; }
            else { plot1.Visibility = Visibility.Hidden; }
            if (temp[1]._isAvailable) { plot2.Visibility = Visibility.Visible; }
            else { plot2.Visibility = Visibility.Hidden; }
            if (temp[2]._isAvailable) { plot3.Visibility = Visibility.Visible; }
            else { plot3.Visibility = Visibility.Hidden; }
            if (temp[3]._isAvailable) { plot4.Visibility = Visibility.Visible; }
            else { plot4.Visibility = Visibility.Hidden; }
            if (temp[4]._isAvailable) { plot5.Visibility = Visibility.Visible; }
            else { plot5.Visibility = Visibility.Hidden; }
            if (temp[5]._isAvailable) { plot6.Visibility = Visibility.Visible; }
            else { plot6.Visibility = Visibility.Hidden; }
            if (temp[6]._isAvailable) { plot7.Visibility = Visibility.Visible; }
            else { plot7.Visibility = Visibility.Hidden; }
            if (temp[7]._isAvailable) { plot7.Visibility = Visibility.Visible; }
            else { plot7.Visibility = Visibility.Hidden; }
            if (temp[8]._isAvailable) { plot9.Visibility = Visibility.Visible; }
            else { plot9.Visibility = Visibility.Hidden; }
        }

        private bool PrefsIsValidated(PlotModelParameter[] temp)
        {
            foreach (PlotModelParameter p in temp)
            {
                if (p._maxValue < p._minValue) return false;
            }
            return true;
        }

        private void tbMinMax_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }
    }
}
