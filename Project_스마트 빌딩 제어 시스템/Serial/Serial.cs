using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;           // Add
using MySql.Data.MySqlClient;

namespace Serial
{
	public partial class tb_id : Form
	{
		public tb_id()
		{
			InitializeComponent();
			getAvailablePorts();     //Add
		}

		//전역변수 설정
		string dataIn;  // Add RS232C from Atmega128A
		string s_date, temper, humid, lx_date, lx, f_date, flame, f_status;
		String strConn = "Server=127.0.0.1;Uid=root;Pwd=1234;Database=sqlDB;CHARSET=UTF8";
		MySqlConnection conn;
		MySqlCommand cmd;
		MySqlDataReader reader;

		private void Serial_Load(object sender, EventArgs e)
		{
			conn = new MySqlConnection(strConn);
			conn.Open();
			cmd = new MySqlCommand("", conn);

			// Monitoring탭 Chart 설정
			chart_sensor.ChartAreas[0].AxisX.Title = "Time";
			chart_sensor.ChartAreas[0].AxisY.Title = "Temper, Humid, Lx";
			chart_sensor.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
			chart_sensor.Series[1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
			chart_sensor.Series[2].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
			chart_sensor.Series[0].Points.Clear();
			chart_sensor.Series[1].Points.Clear();
			chart_sensor.Series[2].Points.Clear();
		}

		private void Serial_FormClosed(object sender, FormClosedEventArgs e)
		{
			conn.Close();
		}

		void getAvailablePorts()
		{
			string[] ports = SerialPort.GetPortNames();
			cb_Port.Items.AddRange(ports);
		}

		private void btn_OpenPort_Click(object sender, EventArgs e)     // Port열기
		{
			try
			{
				if (cb_Port.Text == "" || cb_Bps.Text == "")
				{
					tb_ReceiveData.Text = "Please Select Port Setting!!";
				}
				else
				{
					serialPort1.PortName = cb_Port.Text;
					serialPort1.BaudRate = Convert.ToInt32(cb_Bps.Text);
					serialPort1.Open();
					progressBar_PortStatus.Value = 100;
					lb_ComPort.Text = " ON";
					btn_OpenPort.Enabled = false;
					btn_ClosePort.Enabled = true;
				}
			}
			catch (UnauthorizedAccessException)
			{
				tb_ReceiveData.Text = "UnauthorizedAccessException Occurs!!";
			}
		}

		private void btn_ClosePort_Click(object sender, EventArgs e)    // Port닫기
		{
			serialPort1.Close();
			progressBar_PortStatus.Value = 0;
			lb_ComPort.Text = "OFF";
			btn_OpenPort.Enabled = true;
			btn_ClosePort.Enabled = false;
			tb_ReceiveData.Text = "";
		}

		private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)     // Port Data읽기
		{
			string data = serialPort1.ReadExisting();
			dataIn += data;
			if (data == "\n")
			{
				this.Invoke(new EventHandler(ShowData));
			}
		}

		private void ShowData(object sender, EventArgs e)    // Port Data textbox에 뿌리기
		{
			tb_ReceiveData.AppendText(dataIn);

			//add
			if (rbtn_dht11_on.Checked == true)
			{
				if (dataIn.Contains("temper") && (dataIn.Contains("humid")))
				{
					string[] value = dataIn.Split(':');
					temper = value[1];
					humid = value[3];
					SaveDB();
					lb_Lobby_temper.Text = temper;
					lb_Lobby_humid.Text = humid;
					lb_Toilet_temper.Text = temper;
					lb_Toilet_humid.Text = humid;
					lb_Office_temper.Text = temper;
					lb_Office_humid.Text = humid;
					lb_ConferenceRoom_temper.Text = temper;
					lb_ConferenceRoom_humid.Text = humid;
					lb_1F_temper.Text = temper;
					lb_1F_humid.Text = humid;
					lb_2F_temper.Text = temper;
					lb_2F_humid.Text = humid;
				}
			}
			if (rbtn_lx_on.Checked == true)
			{
				if (dataIn.Contains("Lux"))
				{
					string[] value = dataIn.Split(':');
					lx = value[1];
					SaveDB_lx();

					if (cb_Lobby_eco.Checked == true)
					{
						try
						{
							if (int.Parse(lx) <= 100)
							{
								rb_L2_OFF.Checked = true;
								pictureBox_L2_ON.Visible = false;
								pictureBox_L2_OFF.Visible = true;
							}
							else
							{
								rb_L2_ON.Checked = true;
								pictureBox_L2_ON.Visible = true;
								pictureBox_L2_OFF.Visible = false;
							}
						}
						catch
						{

						}
					}
				}
			}
			if (rbtn_flame_on.Checked == true)
			{
				if (dataIn.Contains("flame"))
				{
					string[] value = dataIn.Split(':');
					flame = value[1];
					lb_Robby_flame_value.Text = flame;
					lb_Toilet_flame_value.Text = flame;
					lb_Office_flame_value.Text = flame;
					lb_ConferenceRoom_flame_value.Text = flame;
					SaveDB_flame();
					try
					{
						if (int.Parse(flame) <= 10)
						{
							lb_Robby_flame.Text = "   정상   ";
							lb_Robby_flame.BackColor = Color.Snow;
							lb_Toilet_flame.Text = "   정상   ";
							lb_Toilet_flame.BackColor = Color.Snow;
							lb_Office_flame.Text = "   정상   ";
							lb_Office_flame.BackColor = Color.Snow;
							lb_ConferenceRoom_flame.Text = "   정상   ";
							lb_ConferenceRoom_flame.BackColor = Color.Snow;
							pictureBox_1F.Visible = true;
							pictureBox_1F_escape.Visible = false;
							pictureBox_2F.Visible = true;
							pictureBox_2F_escape.Visible = false;
						}
						else
						{
							lb_Robby_flame.Text = "화재감지";
							lb_Robby_flame.BackColor = Color.Firebrick;
							lb_Toilet_flame.Text = "화재감지";
							lb_Toilet_flame.BackColor = Color.Firebrick;
							lb_Office_flame.Text = "화재감지";
							lb_Office_flame.BackColor = Color.Firebrick;
							lb_ConferenceRoom_flame.Text = "화재감지";
							lb_ConferenceRoom_flame.BackColor = Color.Firebrick;
							pictureBox_1F.Visible = false;
							pictureBox_1F_escape.Visible = true;
							pictureBox_2F.Visible = false;
							pictureBox_2F_escape.Visible = true;
						}
					}
					catch
					{
					}
				}
			}
			if ((rbtn_dht11_on.Checked == true) && (rbtn_lx_on.Checked == true))
			{
				Draw_Chart_Sensor();
			}
			dataIn = "";
		}

		private void SaveDB()    //Sensor값 DB에 저장
		{
			String sql;
			s_date = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
			sql = "INSERT INTO sensor(s_date, temper, humid) VALUES('";
			sql += s_date + "','" + temper + "','" + humid + "')";
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();
		}
		private void SaveDB_lx()    //lx값 DB에 저장
		{
			String sql;
			lx_date = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
			sql = "INSERT INTO sensor_lx(lx_date, lx) VALUES('";
			sql += lx_date + "','" + lx + "')";
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();
		}
		private void SaveDB_flame()    //flame값 DB에 저장
		{
			try
			{
				if (int.Parse(flame) <= 10)
				{
					f_status = "정상";
				}
				else
				{
					f_status = "화재감지";
				}
			}
			catch (FormatException)
			{
				return;
			}
			String sql;
			f_date = DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
			sql = "INSERT INTO sensor_flame(f_date, flame, f_status) VALUES('";
			sql += f_date + "','" + flame + "','" + f_status + "')";
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();
		}

		private void Draw_Chart_Sensor()    //Sensor값 Chart그리기
		{
			// 차트 그리기
			// 데이터 조회 쿼리
			string s_time;
			string sql = "SELECT s_date, temper, humid, lx FROM sensor INNER JOIN sensor_lx ";
			sql += "ON sensor.s_date = sensor_lx.lx_date ORDER BY s_date DESC LIMIT 1";
			cmd.CommandText = sql;
			try
			{
				reader = cmd.ExecuteReader();
				reader.Read();//어차피 1건
				s_date = reader["s_date"].ToString();
				s_time = s_date.Substring(11);
				temper = reader["temper"].ToString();
				humid = reader["humid"].ToString();
				lx = reader["lx"].ToString();
				reader.Close();

				chart_sensor.Series[0].Points.AddXY(s_time, temper);
				chart_sensor.Series[1].Points.AddXY(s_time, humid);
				chart_sensor.Series[2].Points.AddXY(s_time, lx);
			}
			catch (MySqlException)
			{
				reader.Close();
			}
		}

		private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)   // 리스트뷰
		{
			// history탭 리스트뷰 --> 전체 데이터 조회
			// 데이터 조회 쿼리
			string sql = "SELECT s_date, temper, humid, lx, f_status ";
			sql += "FROM sensor, sensor_lx, sensor_flame where sensor.s_date = sensor_lx.lx_date ";
			sql += "and sensor.s_date = sensor_flame.f_date";
			cmd.CommandText = sql;
			reader = cmd.ExecuteReader();

			// 리스트뷰 설정
			listView_Sensordata.Clear();
			listView_Sensordata.BeginUpdate(); // 리스트뷰 업데이트 시작
			listView_Sensordata.View = View.Details;
			listView_Sensordata.Columns.Add("Date", 150, HorizontalAlignment.Center);
			listView_Sensordata.Columns.Add("Temperature", 100, HorizontalAlignment.Center);
			listView_Sensordata.Columns.Add("Humidity", 100, HorizontalAlignment.Center);
			listView_Sensordata.Columns.Add("Lx", 100, HorizontalAlignment.Center);
			listView_Sensordata.Columns.Add("Flame", 100, HorizontalAlignment.Center);

			ListViewItem item; // 각 행데이터
			while (reader.Read())
			{
				s_date = reader["s_date"].ToString();
				temper = reader["temper"].ToString();
				humid = reader["humid"].ToString();
				lx = reader["lx"].ToString();
				f_status = reader["f_status"].ToString();

				item = new ListViewItem(s_date);
				item.SubItems.Add(temper);
				item.SubItems.Add(humid);
				item.SubItems.Add(lx);
				item.SubItems.Add(f_status);

				listView_Sensordata.Items.Add(item);
			}
			reader.Close();

			listView_Sensordata.EndUpdate(); // 리스트뷰 업데이트 끝

			// history탭 콤보박스 채우기
			Combox_Setting();
		}

		private void Combox_Setting()   // history탭 콤보박스 채우기
		{
			string[] arytime = { };
			string sql = "SELECT s_date, temper, humid, lx, f_status FROM sensor, sensor_lx, sensor_flame ";
			sql += "where sensor.s_date = sensor_lx.lx_date and sensor.s_date = sensor_flame.f_date and s_date";
			cmd.CommandText = sql;
			reader = cmd.ExecuteReader();

			while (reader.Read())
			{
				string timename = (string)reader["s_date"];
				Array.Resize(ref arytime, arytime.Length + 1);
				arytime[arytime.Length - 1] = timename;
			}
			reader.Close();

			//콤보박스에 DB이름 채우기
			cb_start.Items.AddRange(arytime);
			cb_end.Items.AddRange(arytime);
			// 콤보박스 색
			cb_start.BackColor = Color.Khaki;   
			cb_end.BackColor = Color.Khaki;
		}

		private void btn_Select_Click(object sender, EventArgs e)   // history에서 조회버튼
		{
			//데이터 조회 쿼리
			String sql = "SELECT s_date, temper, humid, lx, f_status FROM sensor, sensor_lx, sensor_flame ";
			sql += "where sensor.s_date = sensor_lx.lx_date and sensor.s_date = sensor_flame.f_date and s_date BETWEEN '";
			sql += cb_start.Text + "' AND '" + cb_end.Text + "'";
			cmd.CommandText = sql;
			reader = cmd.ExecuteReader();

			//리스트뷰 설정
			listView_Sensordata.Clear();
			listView_Sensordata.BeginUpdate(); // 리스트뷰 업데이트 시작
			listView_Sensordata.View = View.Details;
			listView_Sensordata.Columns.Add("Date", 150, HorizontalAlignment.Center);
			listView_Sensordata.Columns.Add("Temperature", 100, HorizontalAlignment.Center);
			listView_Sensordata.Columns.Add("Humidity", 100, HorizontalAlignment.Center);
			listView_Sensordata.Columns.Add("Lx", 100, HorizontalAlignment.Center);
			listView_Sensordata.Columns.Add("Flame", 100, HorizontalAlignment.Center);

			ListViewItem item; //각 행데이터
			while (reader.Read())
			{
				s_date = reader["s_date"].ToString();
				temper = reader["temper"].ToString();
				humid = reader["humid"].ToString();
				lx = reader["lx"].ToString();
				f_status = reader["f_status"].ToString();

				item = new ListViewItem(s_date);
				item.SubItems.Add(temper);
				item.SubItems.Add(humid);
				item.SubItems.Add(lx);
				item.SubItems.Add(f_status);

				listView_Sensordata.Items.Add(item);
			}
			reader.Close();
			listView_Sensordata.EndUpdate(); //리스트뷰 업데이트 끝
		}

		private void btn_login_Click(object sender, EventArgs e) //로그인
		{
			string sql;
			try
			{
				sql = "SELECT epassword from employees WHERE eno = '" + tb_eno.Text + "'";
				cmd.CommandText = sql;
				reader = cmd.ExecuteReader();
				reader.Read();
				if (tb_epassword.Text == reader[0].ToString())
				{
					reader.Close();
					panel_Login.Visible = false;
					tabControl1.Visible = true;
				}
			}
			catch
			{
				MessageBox.Show("로그인 정보가 틀렸습니다.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				reader.Close();
				tb_eno.Text = "";
				tb_epassword.Text = "";
				return;
			}
		}

		////////////////////////////////////////////////////////////////////////////////////
		////// GroupBox    /////////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////////
		private void btn_Whole_Click(object sender, EventArgs e)
		{
			groupBox_Whole.Visible = true;
			groupBox_1F.Visible = true;
			groupBox_2F.Visible = true;
			groupBox_Lobby.Visible = false;
			groupBox_Toilet.Visible = false;
			groupBox_Office.Visible = false;
			groupBox_ConferenceRoom.Visible = false;
		}
		private void btn_1F_Click(object sender, EventArgs e)
		{
			groupBox_Whole.Visible = false;
			groupBox_1F.Visible = true;
			groupBox_2F.Visible = false;
			groupBox_Lobby.Visible = false;
			groupBox_Toilet.Visible = false;
			groupBox_Office.Visible = false;
			groupBox_ConferenceRoom.Visible = false;
		}
		private void btn_2F_Click(object sender, EventArgs e)
		{
			groupBox_Whole.Visible = false;
			groupBox_1F.Visible = false;
			groupBox_2F.Visible = true;
			groupBox_Lobby.Visible = false;
			groupBox_Toilet.Visible = false;
			groupBox_Office.Visible = false;
			groupBox_ConferenceRoom.Visible = false;
		}
		private void btn_Robby_Click(object sender, EventArgs e)
		{
			groupBox_Whole.Visible = false;
			groupBox_1F.Visible = false;
			groupBox_2F.Visible = false;
			groupBox_Lobby.Visible = true;
			groupBox_Toilet.Visible = false;
			groupBox_Office.Visible = false;
			groupBox_ConferenceRoom.Visible = false;
		}
		private void btn_Toilet_Click(object sender, EventArgs e)
		{
			groupBox_Whole.Visible = false;
			groupBox_1F.Visible = false;
			groupBox_2F.Visible = false;
			groupBox_Lobby.Visible = false;
			groupBox_Toilet.Visible = true;
			groupBox_Office.Visible = false;
			groupBox_ConferenceRoom.Visible = false;
		}
		private void btn_Office_Click(object sender, EventArgs e)
		{
			groupBox_Whole.Visible = false;
			groupBox_1F.Visible = false;
			groupBox_2F.Visible = false;
			groupBox_Lobby.Visible = false;
			groupBox_Toilet.Visible = false;
			groupBox_Office.Visible = true;
			groupBox_ConferenceRoom.Visible = false;

		}
		private void btn_ConferenceRoom_Click(object sender, EventArgs e)
		{
			groupBox_Whole.Visible = false;
			groupBox_1F.Visible = false;
			groupBox_2F.Visible = false;
			groupBox_Lobby.Visible = false;
			groupBox_Toilet.Visible = false;
			groupBox_Office.Visible = false;
			groupBox_ConferenceRoom.Visible = true;
		}

		////////////////////////////////////////////////////////////////////////////////////
		////// PIR sensor    ///////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////////
		private void rbtn_1F_PIR_ON_CheckedChanged(object sender, EventArgs e)
		{
			serialPort1.WriteLine("pon");
		}

		private void rbtn_1F_PIR_OFF_CheckedChanged(object sender, EventArgs e)
		{
			serialPort1.WriteLine("poff");
		}
		private void rbtn_2F_PIR_ON_CheckedChanged(object sender, EventArgs e)
		{
			//
		}

		private void rbtn_2F_PIR_OFF_CheckedChanged(object sender, EventArgs e)
		{
			//
		}

		////////////////////////////////////////////////////////////////////////////////////
		////// flame sensor    /////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////////
		private void rbtn_frame_on_CheckedChanged(object sender, EventArgs e)
		{
			serialPort1.WriteLine("fon");
		}

		private void rbtn_frame_off_CheckedChanged(object sender, EventArgs e)
		{
			serialPort1.WriteLine("foff");
		}

		////////////////////////////////////////////////////////////////////////////////////
		////// lx sensor    ////////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////////
		private void rbtn_lx_on_CheckedChanged(object sender, EventArgs e)
		{
			serialPort1.WriteLine("lon");
		}
		private void rbtn_lx_off_CheckedChanged(object sender, EventArgs e)
		{
			serialPort1.WriteLine("loff");
		}
		private void cb_Lobby_eco_CheckedChanged(object sender, EventArgs e)
		{
			if (cb_Lobby_eco.Checked == true)
			{
				serialPort1.WriteLine("lxon");
				lb_1F_eco.BackColor = Color.LightGoldenrodYellow;
			}
			if (cb_Lobby_eco.Checked == false)
			{
				serialPort1.WriteLine("lxoff");
				lb_1F_eco.BackColor = Color.Silver;
			}
		}
		private void cb_Office_eco_CheckedChanged(object sender, EventArgs e)
		{
			if (cb_Office_eco.Checked == true)
			{
				serialPort1.WriteLine("lxon");
				lb_2F_eco.BackColor = Color.LightGoldenrodYellow;
			}
			if (cb_Office_eco.Checked == false)
			{
				serialPort1.WriteLine("lxoff");
				lb_2F_eco.BackColor = Color.Silver;
			}
		}
		private void btn_1F_eco_ON_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("lxon");
			lb_1F_eco.BackColor = Color.LightGoldenrodYellow;
			cb_Lobby_eco.Checked = true;
		}
		private void btn_1F_eco_OFF_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("lxoff");
			lb_1F_eco.BackColor = Color.Silver;
			cb_Lobby_eco.Checked = false;
		}
		private void btn_2F_eco_ON_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("lxon");
			lb_2F_eco.BackColor = Color.LightGoldenrodYellow;
			cb_Office_eco.Checked = true;
		}
		private void btn_2F_eco_OFF_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("lxoff");
			lb_2F_eco.BackColor = Color.Silver;
			cb_Office_eco.Checked = false;
		}
		private void btn_Whole_eco_ON_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("lxon");
			lb_1F_eco.BackColor = Color.LightGoldenrodYellow;
			lb_2F_eco.BackColor = Color.LightGoldenrodYellow;
			cb_Lobby_eco.Checked = true;
			cb_Office_eco.Checked = true;
		}
		private void btn_Whole_eco_OFF_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("lxoff");
			lb_1F_eco.BackColor = Color.Silver;
			lb_2F_eco.BackColor = Color.Silver;
			cb_Lobby_eco.Checked = false;
			cb_Office_eco.Checked = false;
		}

		////////////////////////////////////////////////////////////////////////////////////
		////// DHT11 sensor    /////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////////////////
		private void rbtn_dht11_on_CheckedChanged(object sender, EventArgs e)
		{
			serialPort1.WriteLine("on");
		}
		private void rbtn_dht11_off_CheckedChanged(object sender, EventArgs e)
		{
			serialPort1.WriteLine("off");
		}
		private void btn_Lobby_Air_ON_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("airon");
			lb_Lobby_aircon_value1.ForeColor = Color.Black;
			lb_Lobby_aircon_value2.ForeColor = Color.Black;
			btn_Lobby_Air_ON.BackColor = Color.Aquamarine;
			lb_1F_aircon.Text = " ON ";
			lb_1F_aircon.BackColor = Color.Aquamarine;
		}
		private void btn_Lobby_Air_OFF_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("airoff");
			lb_Lobby_aircon_value1.Text = "0";
			lb_Lobby_aircon_value1.ForeColor = Color.Silver;
			lb_Lobby_aircon_value2.ForeColor = Color.Silver;
			btn_Lobby_Air_ON.BackColor = Color.Transparent;
			lb_1F_aircon_value.Text = "0";
			lb_1F_aircon.Text = " OFF ";
			lb_1F_aircon.BackColor = Color.Silver;
			trackBar_Lobby_Air.Value = 0;
		}
		private void trackBar_Lobby_Air_Scroll(object sender, EventArgs e)
		{
			lb_Lobby_aircon_value1.Text = trackBar_Lobby_Air.Value.ToString();
			lb_1F_aircon_value.Text = trackBar_Lobby_Air.Value.ToString();
		}
		private void trackBar_Air_Scroll(object sender, EventArgs e)
		{
			lb_Lobby_aircon_value1.Text = trackBar_Lobby_Air.Value.ToString();
			lb_1F_aircon_value.Text = trackBar_Lobby_Air.Value.ToString();
		}
		private void btn_Office_Air_ON_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("airon");
			lb_Office_aircon_value1.ForeColor = Color.Black;
			lb_Office_aircon_value2.ForeColor = Color.Black;
			btn_Office_Air_ON.BackColor = Color.Aquamarine;
			lb_2F_aircon.Text = " ON ";
			lb_2F_aircon.BackColor = Color.Aquamarine;
		}
		private void btn_Office_Air_OFF_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("airoff");
			lb_Office_aircon_value1.Text = "0";
			lb_Office_aircon_value1.ForeColor = Color.Silver;
			lb_Office_aircon_value2.ForeColor = Color.Silver;
			btn_Office_Air_ON.BackColor = Color.Transparent;
			lb_2F_aircon_value.Text = "0";
			lb_2F_aircon.Text = " OFF ";
			lb_2F_aircon.BackColor = Color.Silver;
			trackBar_Office_Aircon.Value = 0;
		}
		private void trackBar_Office_Aircon_Scroll(object sender, EventArgs e)
		{
			lb_Office_aircon_value1.Text = trackBar_Office_Aircon.Value.ToString();
			lb_2F_aircon_value.Text = trackBar_Office_Aircon.Value.ToString();
		}
		private void btn_1F_Aircon_ON_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("airon");
			lb_Lobby_aircon_value1.ForeColor = Color.Black;
			lb_Lobby_aircon_value2.ForeColor = Color.Black;
			btn_Lobby_Air_ON.BackColor = Color.Aquamarine;
			lb_1F_aircon.Text = " ON ";
			lb_1F_aircon.BackColor = Color.Aquamarine;
		}
		private void btn_1F_Aircon_OFF_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("airoff");
			lb_Lobby_aircon_value1.ForeColor = Color.Silver;
			lb_Lobby_aircon_value2.ForeColor = Color.Silver;
			btn_Lobby_Air_ON.BackColor = Color.Transparent;
			lb_1F_aircon.Text = " OFF ";
			lb_1F_aircon.BackColor = Color.Silver;
		}
		private void btn_2F_Aircon_ON_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("airon");
			lb_Office_aircon_value1.ForeColor = Color.Black;
			lb_Office_aircon_value2.ForeColor = Color.Black;
			btn_Office_Air_ON.BackColor = Color.Aquamarine;
			lb_2F_aircon.Text = " ON ";
			lb_2F_aircon.BackColor = Color.Aquamarine;
		}
		private void btn_2F_Aircon_OFF_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("airoff");
			lb_Office_aircon_value1.ForeColor = Color.Silver;
			lb_Office_aircon_value2.ForeColor = Color.Silver;
			btn_Office_Air_ON.BackColor = Color.Transparent;
			lb_2F_aircon.Text = " OFF ";
			lb_2F_aircon.BackColor = Color.Silver;
		}
		private void btn_Whole_Aircon_ON_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("airon");
			lb_Lobby_aircon_value1.ForeColor = Color.Black;
			lb_Lobby_aircon_value2.ForeColor = Color.Black;
			btn_Lobby_Air_ON.BackColor = Color.Aquamarine;
			lb_1F_aircon.Text = " ON ";
			lb_1F_aircon.BackColor = Color.Aquamarine;
			lb_Office_aircon_value1.ForeColor = Color.Black;
			lb_Office_aircon_value2.ForeColor = Color.Black;
			btn_Office_Air_ON.BackColor = Color.Aquamarine;
			lb_2F_aircon.Text = " ON ";
			lb_2F_aircon.BackColor = Color.Aquamarine;
		}
		private void btn_Whole_Aircon_OFF_Click(object sender, EventArgs e)
		{
			serialPort1.WriteLine("airoff");
			lb_Lobby_aircon_value1.ForeColor = Color.Silver;
			lb_Lobby_aircon_value2.ForeColor = Color.Silver;
			btn_Lobby_Air_ON.BackColor = Color.Transparent;
			lb_1F_aircon.Text = " OFF ";
			lb_1F_aircon.BackColor = Color.Silver;
			lb_Office_aircon_value1.ForeColor = Color.Silver;
			lb_Office_aircon_value2.ForeColor = Color.Silver;
			btn_Office_Air_ON.BackColor = Color.Transparent;
			lb_2F_aircon.Text = " OFF ";
			lb_2F_aircon.BackColor = Color.Silver;
		}

		/////////////////////////////////////////////////////////////////////////////////////
		////// led control       ////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////////////////
		private void rb_led01ON_CheckedChanged_1(object sender, EventArgs e)
		{
			pictureBox_L1_ON.Visible = true;
			pictureBox_L1_OFF.Visible = false;
			serialPort1.WriteLine("led1on");
			lb_L1.Text = " ON ";
			lb_L1.BackColor = Color.Yellow;
		}
		private void rb_led01OFF_CheckedChanged_1(object sender, EventArgs e)
		{
			pictureBox_L1_ON.Visible = false;
			pictureBox_L1_OFF.Visible = true;
			serialPort1.WriteLine("led1off");
			lb_L1.Text = " OFF ";
			lb_L1.BackColor = Color.Silver;
		}
		private void rb_L2_ON_CheckedChanged(object sender, EventArgs e)
		{
			pictureBox_L2_ON.Visible = true;
			pictureBox_L2_OFF.Visible = false;
			serialPort1.WriteLine("led2on");
			lb_L2.Text = " ON ";
			lb_L2.BackColor = Color.Yellow;
		}
		private void rb_L2_OFF_CheckedChanged(object sender, EventArgs e)
		{
			pictureBox_L2_ON.Visible = false;
			pictureBox_L2_OFF.Visible = true;
			serialPort1.WriteLine("led2off");
			lb_L2.Text = " OFF ";
			lb_L2.BackColor = Color.Silver;
		}
		private void rb_L3_ON_CheckedChanged(object sender, EventArgs e)
		{
			pictureBox_L3_ON.Visible = true;
			pictureBox_L3_OFF.Visible = false;
			serialPort1.WriteLine("led3on");
			lb_L3.Text = " ON ";
			lb_L3.BackColor = Color.Yellow;
		}
		private void rb_L3_OFF_CheckedChanged(object sender, EventArgs e)
		{
			pictureBox_L3_ON.Visible = false;
			pictureBox_L3_OFF.Visible = true;
			serialPort1.WriteLine("led3off");
			lb_L3.Text = " OFF ";
			lb_L3.BackColor = Color.Silver;
		}
		private void btn_ledallON_Click(object sender, EventArgs e)
		{
			rb_L1_ON.Checked = true;
			pictureBox_L1_ON.Visible = true;
			pictureBox_L1_OFF.Visible = false;
			lb_L1.Text = " ON ";
			lb_L1.BackColor = Color.Yellow;
			rb_L2_ON.Checked = true;
			pictureBox_L2_ON.Visible = true;
			pictureBox_L2_OFF.Visible = false;
			lb_L2.Text = " ON ";
			lb_L2.BackColor = Color.Yellow;
			rb_L3_ON.Checked = true;
			pictureBox_L3_ON.Visible = true;
			pictureBox_L3_OFF.Visible = false;
			lb_L3.Text = " ON ";
			lb_L3.BackColor = Color.Yellow;
			serialPort1.WriteLine("led123on");
		}
		private void btn_ledallOFF_Click_1(object sender, EventArgs e)
		{
			rb_L1_OFF.Checked = true;
			pictureBox_L1_ON.Visible = false;
			pictureBox_L1_OFF.Visible = true;
			lb_L1.Text = " OFF ";
			lb_L1.BackColor = Color.Silver;
			rb_L2_OFF.Checked = true;
			pictureBox_L2_ON.Visible = false;
			pictureBox_L2_OFF.Visible = true;
			lb_L2.Text = " OFF ";
			lb_L2.BackColor = Color.Silver;
			rb_L3_OFF.Checked = true;
			pictureBox_L3_ON.Visible = false;
			pictureBox_L3_OFF.Visible = true;
			lb_L3.Text = " OFF ";
			lb_L3.BackColor = Color.Silver;
			serialPort1.WriteLine("led123off");
		}
		private void btn_Toilet_ledallON_Click(object sender, EventArgs e)
		{
			pictureBox_L4_ON.Visible = true;
			pictureBox_L4_OFF.Visible = false;
			serialPort1.WriteLine("led4on");
			lb_L4.Text = " ON ";
			lb_L4.BackColor = Color.Yellow;
		}
		private void btn_Toilet_ledallOFF_Click(object sender, EventArgs e)
		{
			pictureBox_L4_ON.Visible = false;
			pictureBox_L4_OFF.Visible = true;
			serialPort1.WriteLine("led4off");
			lb_L4.Text = " OFF ";
			lb_L4.BackColor = Color.Silver;
		}
		private void rbtn_L5_ON_CheckedChanged(object sender, EventArgs e)
		{
			pictureBox_L5_ON.Visible = true;
			pictureBox_L5_OFF.Visible = false;
			serialPort1.WriteLine("led5on");
			lb_L5.Text = " ON ";
			lb_L5.BackColor = Color.Yellow;
		}
		private void rbtn_L5_OFF_CheckedChanged(object sender, EventArgs e)
		{
			pictureBox_L5_ON.Visible = false;
			pictureBox_L5_OFF.Visible = true;
			serialPort1.WriteLine("led5off");
			lb_L5.Text = " OFF ";
			lb_L5.BackColor = Color.Silver;
		}
		private void rbtn_L6_ON_CheckedChanged(object sender, EventArgs e)
		{
			pictureBox_L6_ON.Visible = true;
			pictureBox_L6_OFF.Visible = false;
			serialPort1.WriteLine("led6on");
			lb_L6.Text = " ON ";
			lb_L6.BackColor = Color.Yellow;
		}
		private void rbtn_L6_OFF_CheckedChanged(object sender, EventArgs e)
		{
			pictureBox_L6_ON.Visible = false;
			pictureBox_L6_OFF.Visible = true;
			serialPort1.WriteLine("led6off");
			lb_L6.Text = " OFF ";
			lb_L6.BackColor = Color.Silver;
		}
		private void rbtn_L7_ON_CheckedChanged(object sender, EventArgs e)
		{
			pictureBox_L7_ON.Visible = true;
			pictureBox_L7_OFF.Visible = false;
			serialPort1.WriteLine("led7on");
			lb_L7.Text = " ON ";
			lb_L7.BackColor = Color.Yellow;
		}
		private void rbtn_L7_OFF_CheckedChanged(object sender, EventArgs e)
		{
			pictureBox_L7_ON.Visible = false;
			pictureBox_L7_OFF.Visible = true;
			serialPort1.WriteLine("led7off");
			lb_L7.Text = " OFF ";
			lb_L7.BackColor = Color.Silver;
		}
		private void btn_Office_ledallON_Click(object sender, EventArgs e)
		{
			rbtn_L5_ON.Checked = true;
			pictureBox_L5_ON.Visible = true;
			pictureBox_L5_OFF.Visible = false;
			lb_L5.Text = " ON ";
			lb_L5.BackColor = Color.Yellow;
			rbtn_L6_ON.Checked = true;
			pictureBox_L6_ON.Visible = true;
			pictureBox_L6_OFF.Visible = false;
			lb_L6.Text = " ON ";
			lb_L6.BackColor = Color.Yellow;
			rbtn_L7_ON.Checked = true;
			pictureBox_L7_ON.Visible = true;
			pictureBox_L7_OFF.Visible = false;
			lb_L7.Text = " ON ";
			lb_L7.BackColor = Color.Yellow;
			serialPort1.WriteLine("led567on");
		}
		private void btn_Office_ledallOFF_Click(object sender, EventArgs e)
		{
			rbtn_L5_ON.Checked = false;
			pictureBox_L5_ON.Visible = false;
			pictureBox_L5_OFF.Visible = true;
			lb_L5.Text = " OFF ";
			lb_L5.BackColor = Color.Silver;
			rbtn_L6_ON.Checked = false;
			pictureBox_L6_ON.Visible = false;
			pictureBox_L6_OFF.Visible = true;
			lb_L6.Text = " OFF ";
			lb_L6.BackColor = Color.Silver;
			rbtn_L7_ON.Checked = false;
			pictureBox_L7_ON.Visible = false;
			pictureBox_L7_OFF.Visible = true;
			lb_L7.Text = " OFF ";
			lb_L7.BackColor = Color.Silver;
			serialPort1.WriteLine("led567off");
		}
		private void btn_L8_ON_Click(object sender, EventArgs e)
		{
			pictureBox_L8_ON.Visible = true;
			pictureBox_L8_OFF.Visible = false;
			serialPort1.WriteLine("led8on");
			lb_L8.Text = " ON ";
			lb_L8.BackColor = Color.Yellow;
		}
		private void btn_L8_OFF_Click(object sender, EventArgs e)
		{
			pictureBox_L8_ON.Visible = false;
			pictureBox_L8_OFF.Visible = true;
			serialPort1.WriteLine("led8off");
			lb_L8.Text = " OFF ";
			lb_L8.BackColor = Color.Silver;
		}
		private void btn_1F_LED_ON_Click(object sender, EventArgs e)
		{
			rb_L1_ON.Checked = true;
			pictureBox_L1_ON.Visible = true;
			pictureBox_L1_OFF.Visible = false;
			lb_L1.Text = " ON ";
			lb_L1.BackColor = Color.Yellow;
			rb_L2_ON.Checked = true;
			pictureBox_L2_ON.Visible = true;
			pictureBox_L2_OFF.Visible = false;
			lb_L2.Text = " ON ";
			lb_L2.BackColor = Color.Yellow;
			rb_L3_ON.Checked = true;
			pictureBox_L3_ON.Visible = true;
			pictureBox_L3_OFF.Visible = false;
			lb_L3.Text = " ON ";
			lb_L3.BackColor = Color.Yellow;
			pictureBox_L4_ON.Visible = true;
			pictureBox_L4_OFF.Visible = false;
			lb_L4.Text = " ON ";
			lb_L4.BackColor = Color.Yellow;
			serialPort1.WriteLine("led1fon");
		}
		private void btn_1F_LED_OFF_Click(object sender, EventArgs e)
		{
			rb_L1_OFF.Checked = true;
			pictureBox_L1_ON.Visible = false;
			pictureBox_L1_OFF.Visible = true;
			lb_L1.Text = " OFF ";
			lb_L1.BackColor = Color.Silver;
			rb_L2_OFF.Checked = true;
			pictureBox_L2_ON.Visible = false;
			pictureBox_L2_OFF.Visible = true;
			lb_L2.Text = " OFF ";
			lb_L2.BackColor = Color.Silver;
			rb_L3_OFF.Checked = true;
			pictureBox_L3_ON.Visible = false;
			pictureBox_L3_OFF.Visible = true;
			lb_L3.Text = " OFF ";
			lb_L3.BackColor = Color.Silver;
			pictureBox_L4_ON.Visible = false;
			pictureBox_L4_OFF.Visible = true;
			lb_L4.Text = " OFF ";
			lb_L4.BackColor = Color.Silver;
			serialPort1.WriteLine("led1foff");
		}
		private void btn_2F_LED_ON_Click(object sender, EventArgs e)
		{
			rbtn_L5_ON.Checked = true;
			pictureBox_L5_ON.Visible = true;
			pictureBox_L5_OFF.Visible = false;
			lb_L5.Text = " ON ";
			lb_L5.BackColor = Color.Yellow;
			rbtn_L6_ON.Checked = true;
			pictureBox_L6_ON.Visible = true;
			pictureBox_L6_OFF.Visible = false;
			lb_L6.Text = " ON ";
			lb_L6.BackColor = Color.Yellow;
			rbtn_L7_ON.Checked = true;
			pictureBox_L7_ON.Visible = true;
			pictureBox_L7_OFF.Visible = false;
			lb_L7.Text = " ON ";
			lb_L7.BackColor = Color.Yellow;
			pictureBox_L8_ON.Visible = false;
			pictureBox_L8_OFF.Visible = true;
			lb_L8.Text = " ON ";
			lb_L8.BackColor = Color.Yellow;
			pictureBox_L8_ON.Visible = true;
			pictureBox_L8_OFF.Visible = false;
			serialPort1.WriteLine("led2fon");
		}
		private void btn_2F_LED_OFF_Click(object sender, EventArgs e)
		{
			rbtn_L5_OFF.Checked = true;
			pictureBox_L5_ON.Visible = false;
			pictureBox_L5_OFF.Visible = true;
			lb_L5.Text = " OFF ";
			lb_L5.BackColor = Color.Silver;
			rbtn_L6_OFF.Checked = true;
			pictureBox_L6_ON.Visible = false;
			pictureBox_L6_OFF.Visible = true;
			lb_L6.Text = " OFF ";
			lb_L6.BackColor = Color.Silver;
			rbtn_L7_OFF.Checked = true;
			pictureBox_L7_ON.Visible = false;
			pictureBox_L7_OFF.Visible = true;
			lb_L7.Text = " OFF ";
			lb_L7.BackColor = Color.Silver;
			pictureBox_L8_ON.Visible = false;
			pictureBox_L8_OFF.Visible = true;
			lb_L8.Text = " OFF ";
			lb_L8.BackColor = Color.Silver;
			serialPort1.WriteLine("led2foff");
		}
		private void btn_Whole_LED_ON_Click(object sender, EventArgs e)
		{
			rb_L1_ON.Checked = true;
			pictureBox_L1_ON.Visible = true;
			pictureBox_L1_OFF.Visible = false;
			lb_L1.Text = " ON ";
			lb_L1.BackColor = Color.Yellow;
			rb_L2_ON.Checked = true;
			pictureBox_L2_ON.Visible = true;
			pictureBox_L2_OFF.Visible = false;
			lb_L2.Text = " ON ";
			lb_L2.BackColor = Color.Yellow;
			rb_L3_ON.Checked = true;
			pictureBox_L3_ON.Visible = true;
			pictureBox_L3_OFF.Visible = false;
			lb_L3.Text = " ON ";
			lb_L3.BackColor = Color.Yellow;
			pictureBox_L4_ON.Visible = true;
			pictureBox_L4_OFF.Visible = false;
			lb_L4.Text = " ON ";
			lb_L4.BackColor = Color.Yellow;
			rbtn_L5_ON.Checked = false;
			rbtn_L5_ON.Checked = true;
			pictureBox_L5_ON.Visible = true;
			pictureBox_L5_OFF.Visible = false;
			lb_L5.Text = " ON ";
			lb_L5.BackColor = Color.Yellow;
			rbtn_L6_ON.Checked = true;
			pictureBox_L6_ON.Visible = true;
			pictureBox_L6_OFF.Visible = false;
			lb_L6.Text = " ON ";
			lb_L6.BackColor = Color.Yellow;
			rbtn_L7_ON.Checked = true;
			pictureBox_L7_ON.Visible = true;
			pictureBox_L7_OFF.Visible = false;
			lb_L7.Text = " ON ";
			lb_L7.BackColor = Color.Yellow;
			pictureBox_L8_ON.Visible = true;
			pictureBox_L8_OFF.Visible = false;
			lb_L8.Text = " ON ";
			lb_L8.BackColor = Color.Yellow;
			serialPort1.WriteLine("ledallon");
		}
		private void btn_Whole_LED_OFF_Click(object sender, EventArgs e)
		{
			rb_L1_OFF.Checked = true;
			pictureBox_L1_ON.Visible = false;
			pictureBox_L1_OFF.Visible = true;
			lb_L1.Text = " OFF ";
			lb_L1.BackColor = Color.Silver;
			rb_L2_OFF.Checked = true;
			pictureBox_L2_ON.Visible = false;
			pictureBox_L2_OFF.Visible = true;
			lb_L2.Text = " OFF ";
			lb_L2.BackColor = Color.Silver;
			rb_L3_OFF.Checked = true;
			pictureBox_L3_ON.Visible = false;
			pictureBox_L3_OFF.Visible = true;
			lb_L3.Text = " OFF ";
			lb_L3.BackColor = Color.Silver;
			pictureBox_L4_ON.Visible = false;
			pictureBox_L4_OFF.Visible = true;
			lb_L4.Text = " OFF ";
			lb_L4.BackColor = Color.Silver;
			rbtn_L5_ON.Checked = false;
			pictureBox_L5_ON.Visible = false;
			pictureBox_L5_OFF.Visible = true;
			lb_L5.Text = " OFF ";
			lb_L5.BackColor = Color.Silver;
			rbtn_L6_ON.Checked = false;
			pictureBox_L6_ON.Visible = false;
			pictureBox_L6_OFF.Visible = true;
			lb_L6.Text = " OFF ";
			lb_L6.BackColor = Color.Silver;
			rbtn_L7_ON.Checked = false;
			pictureBox_L7_ON.Visible = false;
			pictureBox_L7_OFF.Visible = true;
			lb_L7.Text = " OFF ";
			lb_L7.BackColor = Color.Silver;
			pictureBox_L8_ON.Visible = false;
			pictureBox_L8_OFF.Visible = true;
			lb_L8.Text = " OFF ";
			lb_L8.BackColor = Color.Silver;
			serialPort1.WriteLine("ledalloff");
		}
	}
}