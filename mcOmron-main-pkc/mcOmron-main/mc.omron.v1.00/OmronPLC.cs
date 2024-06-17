using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace mcOMRON
{
	/// <summary>
	/// Implemented transport's type
	/// </summary>
	public enum TransportType
	{
		Tcp
	};



	/// <summary>
	/// 
	/// Version:	1.0
	/// Author:		Joan Magnet
	/// Date:		02/2015
	/// 
	/// manage communications with an OMRON PLC
	/// 
	/// Implemented FINS commands:
	/// 
	///		- [1,1] MEMORY AREA READ
	///		- [1,2] MEMORY AREA WRITE
	///		- [5,1] CONTROLLER DATA READ
	/// 
	/// Specific DM commands:
	/// 
	///		- ReadDM		(read one DM)
	///		- ReadDMs		(read various DM)
	///		- WriteDM		(write one DM)
	///		- ClearDMs		(clear, set to 0 various DM)
	///		- ReadCIOBit	(read 1 bit from CIO area)
	///		- WriteCioBit	(write one bit at CIO area)
	/// 
	/// </summary>
	public class OmronPLC
	{
		#region **** properties

		/// <summary>
		/// return the connection status
		/// </summary>
		public bool Connected
		{
			get { return this._finsCmd.Connected; }
		}


		/// <summary>
		/// last detected error
		/// </summary>
		public string LastError
		{
			get { return this._finsCmd.LastError; }
		}


		/// <summary>
		/// current FINS command object
		/// </summary>
		public mcOMRON.IFINSCommand FinsCommand
		{
			get { return this._finsCmd; }
		}

		#endregion



		#region **** constructor

		/// <summary>
		/// constructor, a IFinsCommand layer required
		/// </summary>
		/// <param name="finsCommand"></param>
		public OmronPLC(TransportType TType)
		{
			switch(TType)
			{
				case TransportType.Tcp:
					this._finsCmd = new tcpFINSCommand();
					break;
				default:
					throw new Exception("Transport type not defined.");
			}
		}

		#endregion



		#region **** connect & close

		/// <summary>
		/// try to connect with the plc
		/// </summary>
		public bool Connect()
		{
			return this._finsCmd.Connect();
		}



		/// <summary>
		/// close the communication with the plc
		/// </summary>
		public void Close()
		{
			this._finsCmd.Close();
		}

		#endregion



		#region **** FINS commands

		/// <summary>
		/// 
		/// MEMORY AREA READ
		/// 
		/// </summary>
		public bool finsMemoryAreadRead(MemoryArea area, UInt16 address, Byte bit_position, UInt16 count)
		{
			return _finsCmd.MemoryAreaRead(area, address, bit_position, count);
		}



		/// <summary>
		/// 
		/// MEMORY AREA WRITE
		/// 
		/// </summary>
		public bool finsMemoryAreadWrite(MemoryArea area, UInt16 address, Byte bit_position, UInt16 count, Byte[] data)
		{
			return _finsCmd.MemoryAreaWrite(area, address, bit_position, count, ref data);
		}



		/// <summary>
		/// 
		/// CONNECTION DATA READ
		/// 
		/// </summary>
		/// <param name="area"></param>
		/// <returns></returns>
		public bool finsConnectionDataRead(Byte area)
		{
			return _finsCmd.ConnectionDataRead(area);
		}

		#endregion



		#region **** predefined DM commands

		/// <summary>
		/// read one DM
		/// </summary>
		/// <param name="position"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool ReadDM(UInt16 position, ref UInt16 value)
		{
			// FINS command
			//
			if (!finsMemoryAreadRead(MemoryArea.DM, position, 0, 1)) return false;

			// value
			//
			value = BTool.BytesToUInt16(this._finsCmd.Response[0], this._finsCmd.Response[1]);

			return true;
		}



		/// <summary>
		/// read one DM using signed values
		/// </summary>
		/// <param name="position"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool ReadDM(UInt16 position, ref Int16 value)
		{
			// FINS command
			//
			if (!finsMemoryAreadRead(MemoryArea.DM, position, 0, 1)) return false;

			// value
			//
			value = BTool.BytesToInt16(this._finsCmd.Response[0], this._finsCmd.Response[1]);

			return true;
		}



		/// <summary>
		/// read various DM
		/// </summary>
		/// <param name="position"></param>
		/// <param name="data"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public bool ReadDMs(UInt16 position, ref UInt16[] data, UInt16 count)
		{
			// FINS command
			//
			if (!finsMemoryAreadRead(MemoryArea.DM, position, 0, count)) return false;

			// fills the array
			//
			for (int x = 0; x < count; x++)
			{
				data[x] = BTool.BytesToUInt16(this._finsCmd.Response[(x * 2)], this._finsCmd.Response[(x * 2) + 1]);
			}

			return true;
		}



		/// <summary>
		/// write one DM
		/// </summary>
		/// <param name="position"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool WriteDM(UInt16 position, UInt16 value)
		{
			// get the array
			//
			Byte[] cmd = BTool.Uint16toBytes(value);

			// fins command
			//
			return finsMemoryAreadWrite(MemoryArea.DM, position, 0, 1, cmd);
		}

		public bool WriteDM(UInt16 position, UInt16 value, int count)
		{
			// get the array
			//
			Byte[] cmd = BTool.Uint16toBytes(value);

			// fins command
			//
			return finsMemoryAreadWrite(MemoryArea.DM, position, 0, (ushort)count, cmd);
		}

		public bool WriteDM(UInt16 position, UInt16[] values)
		{
			// get the array
			Byte[] cmd = new Byte[values.Length * 2];
			for (int i = 0; i < values.Length; i++)
			{
				Byte[] bytes = BTool.Uint16toBytes(values[i]);
				Array.Copy(bytes, 0, cmd, i * 2, 2);
			}

			// fins command
			return finsMemoryAreadWrite(MemoryArea.DM, position, 0, (ushort)values.Length, cmd);
		}

		/// <summary>
		/// write one DM
		/// </summary>
		/// <param name="position"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool WriteDM(UInt16 position, Int16 value)
		{
			// get the array
			//
			Byte[] cmd = BTool.Int16toBytes(value);

			// fins command
			//
			return finsMemoryAreadWrite(MemoryArea.DM, position, 0, 1, cmd);
		}



		/// <summary>
		/// fills with 0 a mamory area of the PLC
		/// </summary>
		/// <param name="position"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public bool ClearDMs(UInt16 position, UInt16 count)
		{
			// zeroed array (each DM requieres 2 bytes)
			//
			Byte[] cmd = new Byte[count * 2];
			for (int x = 0; x < (count * 2); cmd[x++] = 0) ;

			// fins command
			//
			return finsMemoryAreadWrite(MemoryArea.DM, position, 0, count, cmd);
		}

		#endregion



		#region **** predefined CIO commands

		/// <summary>
		/// reads an specifit bit of CIO area
		/// </summary>
		/// <param name="position"></param>
		/// <param name="bit_position"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool ReadCIOBit(UInt16 position, Byte bit_position, ref Byte value)
		{
			// FINS command
			//
			if (!finsMemoryAreadRead(MemoryArea.CIO_Bit, position, bit_position, 1)) return false;

			// value
			//
			//value = BTool.BytesToUInt16(this._finsCmd.Response[0], this._finsCmd.Response[1]);
			value = this._finsCmd.Response[0];

			return true;
		}



		/// <summary>
		/// write one specific bit of CIO area
		/// </summary>
		/// <param name="position"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool WriteCIOBit(UInt16 position, Byte bit_position, Byte value)
		{
			// get the array
			//
			Byte[] cmd = new Byte[1];
			cmd[0] = value;

			// fins command
			//
			return finsMemoryAreadWrite(MemoryArea.CIO_Bit, position, bit_position, 1, cmd);
		}

		#endregion



		#region **** dialog

		/// <summary>
		/// return last dialog between PC & PLC
		/// </summary>
		/// <param name="Caption"></param>
		/// <returns></returns>
		public string LastDialog(string Caption)
		{
			return this.FinsCommand.LastDialog(Caption);
		}
		
		#endregion		



		#region **** variables
		
		#region **** FINS command

		// FINS command object
		//
		private mcOMRON.IFINSCommand _finsCmd;

		#endregion

		#endregion

		private bool finsMemoryAreaWrite(MemoryArea area, UInt16 position, byte bitPosition, UInt16 count, Byte[] data)
		{
			// Print debug information
			Console.WriteLine("Memory Area: " + area);
			Console.WriteLine("Position: " + position);
			Console.WriteLine("Bit Position: " + bitPosition);
			Console.WriteLine("Count: " + count);
			Console.WriteLine("Data Length: " + data.Length);

			// FINS 명령 구성
			byte[] finsCommand = new byte[18 + data.Length];
			finsCommand[0] = 0x80; // ICF: 정보 제어 필드
			finsCommand[1] = 0x00; // RSV: 예약됨
			finsCommand[2] = 0x02; // GCT: 게이트웨이 통과 카운트
			finsCommand[3] = 0x00; // DNA: 목적지 네트워크 주소
			finsCommand[4] = 0x00; // DA1: 목적지 노드 주소
			finsCommand[5] = 0x00; // DA2: 목적지 유닛 주소
			finsCommand[6] = 0x00; // SNA: 소스 네트워크 주소
			finsCommand[7] = 0x01; // SA1: 소스 노드 주소 (PC의 노드 주소)
			finsCommand[8] = 0x00; // SA2: 소스 유닛 주소
			finsCommand[9] = 0x00; // SID: 서비스 ID
			finsCommand[10] = 0x01; // MRC: 메모리 영역 쓰기 (Main Request Code)
			finsCommand[11] = 0x02; // SRC: 메모리 영역 쓰기 (Sub Request Code)
			finsCommand[12] = (byte)area; // 메모리 영역 코드
			finsCommand[13] = (byte)(position >> 8); // 시작 주소 (상위 바이트)
			finsCommand[14] = (byte)(position & 0xFF); // 시작 주소 (하위 바이트)
			finsCommand[15] = bitPosition; // 비트 주소
			finsCommand[16] = (byte)(count >> 8); // 쓰기할 단어 수 (상위 바이트)
			finsCommand[17] = (byte)(count & 0xFF); // 쓰기할 단어 수 (하위 바이트)

			// 데이터를 FINS 명령에 추가
			Array.Copy(data, 0, finsCommand, 18, data.Length);

			byte[] response = FrameSend(finsCommand);

			// 응답 확인
			if (response.Length > 11 && response[11] == 0x00) // 응답 코드가 0x00이면 성공
			{
				return true;
			}
			else
			{
				//LastError = $"에러 코드: {response[11]:X2}";
				return false;
			}
		}


		public byte[] FrameSend(byte[] finsCommand)
		{
			byte[] response = new byte[256]; // 응답 데이터를 저장할 배열

			try
			{
				using (TcpClient tcpClient = new TcpClient("192.168.1.10", 9636))
				using (NetworkStream stream = tcpClient.GetStream())
				{
					// FINS 명령 전송
					stream.Write(finsCommand, 0, finsCommand.Length);
					Console.WriteLine("FINS 명령이 전송되었습니다.");

					// 응답 수신
					int bytesRead = stream.Read(response, 0, response.Length);
					Array.Resize(ref response, bytesRead); // 실제 읽은 바이트 수에 맞게 배열 크기 조정
				}
			}
			catch (Exception ex)
			{
				//LastError = ex.Message;
			}

			return response;
		}

		
	}
}

