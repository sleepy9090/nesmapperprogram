using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

// NES ROM UTILITY 2.0
// Programmed by: Shawn M. Crawford

namespace nesmapperprogram
{
    public partial class mainForm : Form
    {
        public mainForm()
        {
            InitializeComponent();
        }

        #region global variables
        string openFlagString;
        string filename;
        string path;

        private static readonly Dictionary<char, string> hexCharacterToBinary = new Dictionary<char, string> {
            { '0', "0000" },
            { '1', "0001" },
            { '2', "0010" },
            { '3', "0011" },
            { '4', "0100" },
            { '5', "0101" },
            { '6', "0110" },
            { '7', "0111" },
            { '8', "1000" },
            { '9', "1001" },
            { 'a', "1010" },
            { 'b', "1011" },
            { 'c', "1100" },
            { 'd', "1101" },
            { 'e', "1110" },
            { 'f', "1111" }
        };
        #endregion

        #region delegates
        private delegate void puttext(object sender, string p);
        private delegate void putvalue(object sender, int value);
        #endregion

        #region private methods
        private void put(object sender, string p)
        {
            if (sender == (object)statusLabel)
            {
                ((Control)sender).Text = p;
            }
            else
            {
                ((Control)sender).Text += p;
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (openFlagString == "0")
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Open file...";
                ofd.Filter = "NES files (*.nes)|*.nes|BIN files(*.bin)|*.bin|All files (*.*)|*.*";
                ofd.Multiselect = false;
                 
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    nesFileNameTextBox.Text = ofd.FileName;
                }
            }

            // TODO: This code is ugly. Fix it.
            if (nesFileNameTextBox.Text != "")
            {
                path = nesFileNameTextBox.Text;
                filename = nesFileNameTextBox.Text.Substring(nesFileNameTextBox.Text.LastIndexOf('\\') + 1);

                //enable the options after rom is loaded
                remove16byteHeaderCheckBox.Enabled = true;
                analyzeRomButton.Enabled = true;
                analyzeRomToolStripMenuItem.Enabled = true;
                cleanRomButton.Enabled = true;
                //prepRomButton.Enabled = true;

                // Clear out all the fields
                mapperTextBox.Text = "";
                mirroringTextBox.Text = "";
                prgSizeTextBox.Text = "";
                chrSizeTextBox.Text = "";
                trainerTextBox.Text = "";
                textBoxFourScreenMode.Text = "";
                batteryTextBox.Text = "";
                exampleRomsTextBox.Text = "";
                prgSizeSplitTextBox.Text = "";
                chrSizeSplitTextBox.Text = "";
                remove16byteHeaderCheckBox.Checked = false;
                outputCHRPRGCheckBox.Checked = false;

                //titleLabel.Text = "File: [ " + filename + " ]";
                romNameTextBox.Text = filename;
                //off = 0;

                asciiTextBox.Text = "";
                hexTextBox.Text = "";

                loadfile();

                // Analyze the ROM so the user does't have to click the button
                analyzeRom();
            }

            //reset the openflag
            openFlagString = "0";
        }

        private void loadfile()
        {
            using (StreamReader streamReader = new StreamReader(path, Encoding.Default))
            {
                char[] buffer1;
                string asciiString;

                int counterOneInt, counterTwoInt;
                //start first loop
                while (!streamReader.EndOfStream)
                {
                    buffer1 = new char[1024 * 1024 + 1];
                    int size2 = streamReader.ReadBlock(buffer1, 0, 1024 * 1024);

                    MemoryStream memoryStream = new MemoryStream(size2 + 1);
                    MemoryStream memoryStream2 = new MemoryStream(size2 + 1);

                    StreamWriter streanWriter = new StreamWriter(memoryStream);
                    StreamWriter streamWriter2 = new StreamWriter(memoryStream2);
                    counterTwoInt = 0;

                    //start second loop
                    while (counterTwoInt < 16)
                    {
                        int size = 1;

                        char[] buffer2 = new char[] 
                        { 
                            buffer1[counterTwoInt] 
                        };

                        counterOneInt = 0;
                        //this splits into 2 bytes 00 01 02 E4 etc
                        while (counterOneInt < size)
                        {
                            //g = ascii key char value?
                            byte g = Encoding.Default.GetBytes(buffer2, counterOneInt, 1)[0];
                            //n = hex value of the ascii chars
                            string n = g.ToString("x").ToUpper();

                            //if n = 0 then n will = 00
                            if (n.Length == 1) n = "0" + n;
                            //write the hex line
                            streanWriter.Write(n);

                            //if ascii value of g is between 33 and 122 then n = 00
                            if (!(g >= 33 && g <= 122)) n = "00";

                            //if n = 00 then ascii line is .
                            if (n == "00")
                            {
                                streamWriter2.Write(".");
                            }
                            else
                            //else ascii line = ascii value of the hex string
                            {
                                asciiString = Encoding.Default.GetString(new byte[] { g });
                                streamWriter2.Write(asciiString);
                            }

                            counterOneInt++;
                        }
                        //end third loop
                        counterTwoInt++;
                        streanWriter.Write(" ");

                    }
                    //end second loop
                    streanWriter.Flush();
                    streamWriter2.Flush();
                    memoryStream.Flush();
                    memoryStream2.Flush();

                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream2.Seek(0, SeekOrigin.Begin);

                    StreamReader streamReader2 = new StreamReader(memoryStream, Encoding.Default);
                    hexTextBox.Invoke(new puttext(put), new object[] { hexTextBox, streamReader2.ReadToEnd() });
                    streamReader2.Close();

                    streamReader2 = new StreamReader(memoryStream2, Encoding.Default);
                    asciiTextBox.Invoke(new puttext(put), new object[] { asciiTextBox, streamReader2.ReadToEnd() });
                    streamReader2.Close();
                }
            }
        }

        private void prepRomButton_Click(object sender, EventArgs e)
        {
            // TODO: Convert to "using" for the streams

            if (remove16byteHeaderCheckBox.Checked)
            {
                // filepath is now the path to the nes rom
                string filePath = @path;
                long offset = Convert.ToInt64("14");
                int x = 0;
                int y = @path.Length;
                int errorHandler = 0;
                bool wasPRGRipped = false;
                bool wasCHRRipped = false;
                bool was8KBPRGChunked = false;
                bool was8KBCHRChunked = false;

                #region Remove 16 byte header
                //read the nes rom in binary mode
                FileStream fileStream16 = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                BinaryReader binaryReader16 = new BinaryReader(fileStream16, Encoding.Default);

                // Read the ROM into nesData
                byte[] nesData = new byte[binaryReader16.BaseStream.Length];
                nesData = binaryReader16.ReadBytes((int)binaryReader16.BaseStream.Length);
                
                //output the file at the same location and append .bin to it
                FileStream outStream = File.Create(@path + ".bin");
                BinaryWriter binaryWriter16 = new BinaryWriter(outStream);

                try
                {
                    // we need to set x to 16 to remove the header then add fs.length - 16
                    // write the data from nesData (the ROM) to the outstream (the BIN) minus the header
                    x = 16;
                    while ((binaryWriter16.BaseStream.Position) < (fileStream16.Length - 16))
                    {
                        binaryWriter16.Write(nesData[x]);
                        x = x + 1;
                    }
                } 
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to remove the header:\r\n" + ex, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    errorHandler = 1;
                }
                finally
                {
                    outStream.Close();
                    fileStream16.Close();
                    binaryReader16.Close();
                    binaryWriter16.Close();
                }

                outStream.Close();
                fileStream16.Close();
                binaryReader16.Close();
                binaryWriter16.Close();
                #endregion

                if (outputCHRPRGCheckBox.Checked && errorHandler == 0)
                {
                    // ensure CHR/PRG is numeric
                    int prgSize = 0;
                    int chrSize = 0;
                    if (int.TryParse(prgSizeSplitTextBox.Text.Trim(), out prgSize)
                        && int.TryParse(chrSizeSplitTextBox.Text.Trim(), out chrSize)
                        && !eightKBSplitPrgChrRadioButton.Checked)
                    {
                        string updatedFilePath = @path + ".bin";
                        int xx = 0;

                        #region Extract PRG to file
                        if (Convert.ToInt32(prgSizeSplitTextBox.Text) != 0)
                        {
                            //Split the rom into PRG & CHR

                            //read the nes rom in binary mode
                            FileStream fileStream = new FileStream(updatedFilePath, FileMode.Open, FileAccess.Read);
                            BinaryReader binaryReader = new BinaryReader(fileStream, Encoding.Default);

                            byte[] nesPrgData = new byte[binaryReader.BaseStream.Length];
                            nesPrgData = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);

                            //chop out the prg, we are not using the ROM anymore, we are using the BIN we created from the ROM
                            //output the file at the same location and append .prg.bin to it
                            FileStream prgOutStream = File.Create(@path + ".prg.bin");
                            BinaryWriter binaryWriter = new BinaryWriter(prgOutStream);

                            try
                            {
                                while ((binaryWriter.BaseStream.Position) != prgSize)
                                {
                                    binaryWriter.Write(nesPrgData[xx]);
                                    xx++;
                                }
                                wasPRGRipped = true;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("The rom did not split correctly (Incorrect PRG Size?):\r\n" + ex, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                errorHandler = 1;
                            }
                            finally
                            {
                                fileStream.Close();
                                binaryReader.Close();
                                binaryWriter.Close();
                            }

                            fileStream.Close();
                            binaryReader.Close();
                            binaryWriter.Close();
                        }
                        #endregion

                        #region Extract CHR to file
                        if (Convert.ToInt32(chrSizeSplitTextBox.Text) != 0)
                        {
                            //chop out the chr, we are not using the ROM anymore, we are using the BIN we created from the ROM
                            FileStream fileStream = new FileStream(updatedFilePath, FileMode.Open, FileAccess.Read);
                            BinaryReader binaryReader = new BinaryReader(fileStream, Encoding.Default);

                            byte[] nesChrData = new byte[binaryReader.BaseStream.Length];
                            nesChrData = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
                            FileStream chrOutStream = File.Create(@path + ".chr.bin");
                            BinaryWriter binaryWriter = new BinaryWriter(chrOutStream);

                            try
                            {
                                int counter = chrSize;
                                while (0 < counter)
                                {
                                    binaryWriter.Write(nesChrData[xx]);
                                    xx++; // data position
                                    counter--;
                                }
                                wasCHRRipped = true;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("The rom did not split correctly (Incorrect CHR Size?):\r\n" + ex, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                errorHandler = 1;
                            }
                            finally
                            {
                                fileStream.Close();
                                binaryReader.Close();
                                binaryWriter.Close();
                            }

                            fileStream.Close();
                            binaryReader.Close();
                            binaryWriter.Close();
                        }
                        #endregion

                    }
                    #region Extract 8KB for Vs. Unisystem
                        // Up to 4 ROMS are PRG, up to 2 ROMS are CHR
                    else if (int.TryParse(prgSizeSplitTextBox.Text.Trim(), out prgSize)
                        && int.TryParse(chrSizeSplitTextBox.Text.Trim(), out chrSize) 
                        && eightKBSplitPrgChrRadioButton.Checked)
                    {
                        /* Warning for invalid CHR/PRG */
                        //int chrSize = Convert.ToInt32(chrSizeSplitTextBox.Text);
                        //int prgSize = Convert.ToInt32(prgSizeSplitTextBox.Text);

                        // Games with 16K PRG and 8K of CHR should fit. Theoretically 32k PRG / 16k CHR
                        if (chrSize > 16384 || prgSize > 32768)
                        {
                            StringBuilder warningMessageStringBuilder = new StringBuilder("");

                            warningMessageStringBuilder.Append("Vs. Unisystem are meant to use up to four 8K PRG ROMs and up to two 8k CHR ROMs. Games with 16K PRG and 8K CHR or 32K PRG and 16K of CHR should work.\r\n\r\n");

                            warningMessageStringBuilder.Append("Warning:\r\n\r\n");
                            if (prgSize > 32768)
                            {
                                warningMessageStringBuilder.Append("PRG size exceeds the maximum size for a Vs. Unisystem without a daughter board.\r\n");
                                warningMessageStringBuilder.Append("Only the first four 8K chunks will be extracted.\r\n\r\n");
                            }

                            if (chrSize > 16384)
                            {
                                warningMessageStringBuilder.Append("CHR size exceeds the maximum size for a Vs. Unisystem without a daughter board.\r\n");
                                warningMessageStringBuilder.Append("Only the first two 8K chunks will be extracted.\r\n\r\n");
                            }

                            MessageBox.Show(warningMessageStringBuilder.ToString(), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        /* End Warning */

                        string updatedFilePath = @path + ".bin";
                        int xx = 0;

                        #region Extract 8KB PRG for Vs. Unisystem
                        if (Convert.ToInt32(prgSizeSplitTextBox.Text) != 0)
                        {
                            //Split the rom into 8KB chunks
                            //read the nes rom in binary mode
                            FileStream fileStream = new FileStream(updatedFilePath, FileMode.Open, FileAccess.Read);
                            BinaryReader binaryReader = new BinaryReader(fileStream, Encoding.Default);

                            byte[] nesVsPRGData = new byte[binaryReader.BaseStream.Length];
                            nesVsPRGData = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);

                            // a,b,c, or d
                            char c = getCharForVsUnisystemPRGNaming(prgSize);
                            int breakOutOfLoopCounter = 0;

                            // We are not using the ROM anymore, we are using the BIN we created from the ROM
                            // output the file at the same location and append .1a.bin, .1b.bin... to it
                            FileStream nesVsDataOutStream = File.Create(@path + ".prg.1" + c + ".bin"); //1a, 1b, 1c, 1d, this should only be for 24KB or 32KB ROMS, but leaving it to split any ROM into 8KB chunks for experiments
                            FileStream nesVsDataOutStream2 = File.Create(@path + ".prg.6" + c + ".bin");
                            BinaryWriter binaryWriter = new BinaryWriter(nesVsDataOutStream);
                            BinaryWriter binaryWriter2 = new BinaryWriter(nesVsDataOutStream2);

                            try
                            {
                                int counter = (int)binaryWriter.BaseStream.Position;
                                //while ((counter) < nesVsData.Length)
                                while (counter < prgSize)
                                {
                                    // Change the output file every 8192 KB
                                    int currentPosition = ((int)binaryWriter.BaseStream.Position);
                                    int remainder = currentPosition % 8192;
                                    if (remainder == 0 && currentPosition != 0)
                                    {
                                        c--;
                                        breakOutOfLoopCounter++;
                                        // When we do this, the binarywriter position resets to 0
                                        nesVsDataOutStream.Close();
                                        nesVsDataOutStream2.Close();
                                        binaryWriter.Close();
                                        binaryWriter2.Close();
                                        if (breakOutOfLoopCounter != 4)
                                        {
                                            nesVsDataOutStream = File.Create(@path + ".prg.1" + c + ".bin");
                                            nesVsDataOutStream2 = File.Create(@path + ".prg.6" + c + ".bin");
                                            binaryWriter = new BinaryWriter(nesVsDataOutStream);
                                            binaryWriter2 = new BinaryWriter(nesVsDataOutStream2);
                                        }
                                    }

                                    // max of 4 ROM files, rom d, rom c, rom b, and rom a
                                    if (breakOutOfLoopCounter == 4)
                                    {
                                        break;
                                    }

                                    binaryWriter.Write(nesVsPRGData[xx]);
                                    binaryWriter2.Write(nesVsPRGData[xx]);
                                    xx++;
                                    counter++;
                                }
                                was8KBPRGChunked = true;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("The rom did not split correctly (Incorrect PRG Size?):\r\n" + ex, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                errorHandler = 1;
                            }
                            finally
                            {
                                fileStream.Close();
                                nesVsDataOutStream.Close();
                                nesVsDataOutStream2.Close();
                                binaryReader.Close();
                                binaryWriter.Close();
                                binaryWriter2.Close();
                            }

                            fileStream.Close();
                            nesVsDataOutStream.Close();
                            nesVsDataOutStream2.Close();
                            binaryReader.Close();
                            binaryWriter.Close();
                            binaryWriter2.Close();
                        }
                        #endregion

                        #region Extract 8KB CHR for Vs. Unisystem
                        if (Convert.ToInt32(chrSizeSplitTextBox.Text) != 0)
                        {
                            //string updatedFilePath = @path + ".bin";
                            
                            // Don't reset the data position
                            //int xx = 0;

                            //Split the rom into 8KB chunks
                            //read the nes rom in binary mode
                            FileStream fileStream = new FileStream(updatedFilePath, FileMode.Open, FileAccess.Read);
                            BinaryReader binaryReader = new BinaryReader(fileStream, Encoding.Default);

                            byte[] nesVsCHRData = new byte[binaryReader.BaseStream.Length];
                            nesVsCHRData = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);

                            // this iteration always beings with 'b'
                            char c = getCharForVsUnisystemCHRNaming(chrSize);
                            int breakOutOfLoopCounter = 0;

                            // We are not using the ROM anymore, we are using the BIN we created from the ROM
                            // output the file at the same location and append .2a.bin, .b.bin... to it
                            FileStream nesVsDataOutStream = File.Create(@path + ".chr.2" + c + ".bin"); //2a, 2b, 2c, 2d, this should only be for 24KB or 32KB ROMS, but leaving it to split any ROM into 8KB chunks for experiments
                            FileStream nesVsDataOutStream2 = File.Create(@path + ".chr.8" + c + ".bin");
                            BinaryWriter binaryWriter = new BinaryWriter(nesVsDataOutStream);
                            BinaryWriter binaryWriter2 = new BinaryWriter(nesVsDataOutStream2);
                            
                            try
                            {
                                //int counter = (int)binaryWriter.BaseStream.Position;
                                //while ((counter) < nesVsCHRData.Length)
                                int counter = chrSize;
                                while (0 < counter)
                                {
                                    // Change the output file every 8192 KB
                                    int currentPosition = ((int)binaryWriter.BaseStream.Position);
                                    int remainder = currentPosition % 8192;
                                    if (remainder == 0 && currentPosition != 0)
                                    {
                                        c--;
                                        breakOutOfLoopCounter++;
                                        // When we do this, the binarywriter position resets to 0
                                        nesVsDataOutStream.Close();
                                        nesVsDataOutStream2.Close();
                                        binaryWriter.Close();
                                        binaryWriter2.Close();

                                        // max of 2 ROM files, rom b and rom a
                                        if (breakOutOfLoopCounter != 2)
                                        {
                                            nesVsDataOutStream = File.Create(@path + ".chr.2" + c + ".bin");
                                            nesVsDataOutStream2 = File.Create(@path + ".chr.8" + c + ".bin");
                                            binaryWriter = new BinaryWriter(nesVsDataOutStream);
                                            binaryWriter2 = new BinaryWriter(nesVsDataOutStream2);
                                        }
                                    }

                                    // max of 2 ROM files, rom b and rom a
                                    if (breakOutOfLoopCounter == 2)
                                    {
                                        break;
                                    }

                                    binaryWriter.Write(nesVsCHRData[xx]);
                                    binaryWriter2.Write(nesVsCHRData[xx]);
                                    xx++;
                                    counter--;
                                }
                                was8KBCHRChunked = true;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("The rom did not split correctly (Incorrect CHR Size?):\r\n" + ex, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                errorHandler = 1;
                            }
                            finally
                            {
                                fileStream.Close();
                                nesVsDataOutStream.Close();
                                nesVsDataOutStream2.Close();
                                binaryReader.Close();
                                binaryWriter.Close();
                                binaryWriter2.Close();
                            }

                            fileStream.Close();
                            nesVsDataOutStream.Close();
                            nesVsDataOutStream2.Close();
                            binaryReader.Close();
                            binaryWriter.Close();
                            binaryWriter2.Close();
                        }
                        #endregion
                    }
                    #endregion
                    else
                    {
                        MessageBox.Show("PRG or CHR value was invalid.", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        errorHandler = 1;
                    }
                }

                if (errorHandler == 0)
                {
                    filename = nesFileNameTextBox.Text.Substring(nesFileNameTextBox.Text.LastIndexOf('\\') + 1);
                    StringBuilder messageSB = new StringBuilder();
                    messageSB.Append("Un-headered ROM saved as: " + filename + ".bin\r\n");

                    if (wasPRGRipped)
                    {
                        messageSB.Append("\r\nPRG Bin saves as: " + filename + ".prg.bin\r\n");
                    }

                    if (wasCHRRipped)
                    {
                        messageSB.Append("\r\nCHR Bin saved as: " + filename + ".chr.bin\r\n");
                    }

                    if (was8KBPRGChunked)
                    {
                        messageSB.Append("\r\nVs. Unisystem PRG Bins saved as:\r\n" + filename + ".prg.1a.bin, .prg.1b.bin, .prg.1c.bin, ...\r\n" + filename + ".prg.6a.bin, .prg.6b.bin, .prg.6c.bin, ...\r\n");
                    }

                    if (was8KBCHRChunked)
                    {
                        messageSB.Append("\r\nVs. Unisystem CHR Bins saved as:\r\n" + filename + ".chr.2a.bin, .chr.2b.bin, .chr.2c.bin, ...\r\n" + filename + ".chr.8a.bin, .chr.8b.bin, .chr.8c.bin, ...\r\n");
                    }

                    MessageBox.Show("Operation Successfully Completed!\r\n\r\n" + messageSB.ToString(), "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void analyzeRomButton_Click(object sender, EventArgs e)
        {
            analyzeRom();
        }

        private char getCharForVsUnisystemPRGNaming(int prgSize)
        {
            // 8kb/8kb
            char c = 'a';

            // if PRG is 16kb then the ROM for CHR goes in slot b. (16kb/8kb)
            if (prgSize == 16384)
            {
                c = 'b';
            }
            else if (prgSize == 24567)
            {
                c = 'c';
            }
            else if (prgSize >= 32768)
            {
                c = 'd';
            }

            return c;
        }

        private char getCharForVsUnisystemCHRNaming(int chrSize)
        {
            // This is always 'b'
            return 'b';

            //// 8kb/8kb
            //char c = 'a';

            //// if PRG is 16kb then the ROM for CHR goes in slot b. (16kb/8kb)
            //if (chrSize == 16384)
            //{
            //    c = 'b';
            //}

            //if (chrSize == 24567)
            //{
            //    c = 'c';
            //}

            //if (chrSize == 32768)
            //{
            //    c = 'd';
            //}

            //return c;
        }

        private void analyzeRom()
        {
            #region variables
            bool nes20Supported = false;

            //int hexHeaderArrayCounterInt = 0;
            int mapperInt = 0;
            
            // This is for the upper and lower nibble, as a hex string, for the header (0 to 15 bytes)
            string upperNibbleByteZeroString = "";
            string lowerNibbleByteZeroString = "";
            string upperNibbleByteOneString = "";
            string lowerNibbleByteOneString = "";
            string upperNibbleByteTwoString = "";
            string lowerNibbleByteTwoString = "";
            string upperNibbleByteThreeString = "";
            string lowerNibbleByteThreeString = "";
            string upperNibbleByteFourString = "";
            string lowerNibbleByteFourString = "";
            string upperNibbleByteFiveString = "";
            string lowerNibbleByteFiveString = "";
            string upperNibbleByteSixString = "";
            string lowerNibbleByteSixString = "";
            string upperNibbleByteSevenString = "";
            string lowerNibbleByteSevenString = "";
            string upperNibbleByteEightString = "";
            string lowerNibbleByteEightString = "";
            string upperNibbleByteNineString = "";
            string lowerNibbleByteNineString = "";
            string upperNibbleByteTenString = "";
            string lowerNibbleByteTenString = "";
            string upperNibbleByteElevenString = "";
            string lowerNibbleByteElevenString = "";
            string upperNibbleByteTwelveString = "";
            string lowerNibbleByteTwelveString = "";
            string upperNibbleByteThirteenString = "";
            string lowerNibbleByteThirteenString = "";
            string upperNibbleByteFourteenString = "";
            string lowerNibbleByteFourteenString = "";
            string upperNibbleByteFifteenString = "";
            string lowerNibbleByteFifteenString = "";

            string hexInformationForArrayString = "";
            string mapperHexString = "";
            string mapperDecString = "";
            string chrSizeString = "";
            string prgSizeString = "";

            hexInformationForArrayString = hexTextBox.Text;

            // Get rid of spaces
            hexInformationForArrayString = hexInformationForArrayString.Replace(" ", String.Empty);
            
            // Create a char array
            char[] hc = hexInformationForArrayString.ToCharArray();

            // Assign the values
            upperNibbleByteZeroString = hc[0].ToString();
            lowerNibbleByteZeroString = hc[1].ToString();
            upperNibbleByteOneString = hc[2].ToString();
            lowerNibbleByteOneString = hc[3].ToString();
            upperNibbleByteTwoString = hc[4].ToString();
            lowerNibbleByteTwoString = hc[5].ToString();
            upperNibbleByteThreeString = hc[6].ToString();
            lowerNibbleByteThreeString = hc[7].ToString();
            upperNibbleByteFourString = hc[8].ToString();
            lowerNibbleByteFourString = hc[9].ToString();
            upperNibbleByteFiveString = hc[10].ToString();
            lowerNibbleByteFiveString = hc[11].ToString();
            upperNibbleByteSixString = hc[12].ToString();
            lowerNibbleByteSixString = hc[13].ToString();
            upperNibbleByteSevenString = hc[14].ToString();
            lowerNibbleByteSevenString = hc[15].ToString();
            upperNibbleByteEightString = hc[16].ToString();
            lowerNibbleByteEightString = hc[17].ToString();
            upperNibbleByteNineString = hc[18].ToString();
            lowerNibbleByteNineString = hc[19].ToString();
            upperNibbleByteTenString = hc[20].ToString();
            lowerNibbleByteTenString = hc[21].ToString();
            upperNibbleByteElevenString = hc[22].ToString();
            lowerNibbleByteElevenString = hc[23].ToString();
            upperNibbleByteTwelveString = hc[24].ToString();
            lowerNibbleByteTwelveString = hc[25].ToString();
            upperNibbleByteThirteenString = hc[26].ToString();
            lowerNibbleByteThirteenString = hc[27].ToString();
            upperNibbleByteFourteenString = hc[28].ToString();
            lowerNibbleByteFourteenString = hc[29].ToString();
            upperNibbleByteFifteenString = hc[30].ToString();
            lowerNibbleByteFifteenString = hc[31].ToString();
            #endregion

            #region PRG split info

            /*
                4 = PRG (Hex number depends on size of PRG file)-/-- (See Sect. 4)

                [PRG - (Range 1 x 16kb pages -> 64 x 16kb pages)]
                -There is a BARE MINIMUM required for PRG which is 1 x 16kb pages!

                * NOTE: To figure out the exact size in bytes each of these pages are worth
                        just start at 1 x 16kb pages (aka: 16384 bytes) and just keep adding
                        16384 more for each "page" higher.

                (1 x 16kb pages)  = 01     (2 x 16kb pages)  = 02     (3 x 16kb pages)  = 03
                (4 x 16kb pages)  = 04     (5 x 16kb pages)  = 05     (6 x 16kb pages)  = 06
                (7 x 16kb pages)  = 07     (8 x 16kb pages)  = 08     (9 x 16kb pages)  = 09
                (10 x 16kb pages) = 0A     (11 x 16kb pages) = 0B     (12 x 16kb pages) = 0C
                (13 x 16kb pages) = 0D     (14 x 16kb pages) = 0E     (15 x 16kb pages) = 0F
                (16 x 16kb pages) = 10     (17 x 16kb pages) = 11     (18 x 16kb pages) = 12
                (19 x 16kb pages) = 13     (20 x 16kb pages) = 14     (21 x 16kb pages) = 15
                (22 x 16kb pages) = 16     (23 x 16kb pages) = 17     (24 x 16kb pages) = 18
                (25 x 16kb pages) = 19     (26 x 16kb pages) = 1A     (27 x 16kb pages) = 1B
                (28 x 16kb pages) = 1C     (29 x 16kb pages) = 1D     (30 x 16kb pages) = 1E
                (31 x 16kb pages) = 1F     (32 x 16kb pages) = 20     (33 x 16kb pages) = 21
                (34 x 16kb pages) = 22     (35 x 16kb pages) = 23     (36 x 16kb pages) = 24
                (37 x 16kb pages) = 25     (38 x 16kb pages) = 26     (39 x 16kb pages) = 27
                (40 x 16kb pages) = 28     (41 x 16kb pages) = 29     (42 x 16kb pages) = 2A
                (43 x 16kb pages) = 2B     (44 x 16kb pages) = 2C     (45 x 16kb pages) = 2D
                (46 x 16kb pages) = 2E     (47 x 16kb pages) = 2F     (48 x 16kb pages) = 30 
                (49 x 16kb pages) = 31     (50 x 16kb pages) = 32     (51 x 16kb pages) = 33
                (52 x 16kb pages) = 34     (53 x 16kb pages) = 35     (54 x 16kb pages) = 36
                (55 x 16kb pages) = 37     (56 x 16kb pages) = 38     (57 x 16kb pages) = 39
                (58 x 16kb pages) = 3A     (59 x 16kb pages) = 3B     (60 x 16kb pages) = 3C
                (61 x 16kb pages) = 3D     (62 x 16kb pages) = 3E     (63 x 16kb pages) = 3F
                (64 x 16kb pages) = 40
            */

            prgSizeString = upperNibbleByteFourString + lowerNibbleByteFourString;

            switch (prgSizeString)
            {
                case "00":
                    prgSizeTextBox.Text = "0kb (0 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0000000";
                    break;
                case "01":
                    prgSizeTextBox.Text = "16kb (1 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0016384";
                    break;
                case "02":
                    prgSizeTextBox.Text = "32kb (2 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0032768";
                    break;
                case "03":
                    prgSizeTextBox.Text = "48kb (3 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0049152";
                    break;
                case "04":
                    prgSizeTextBox.Text = "64kb (4 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0065536";
                    break;
                case "05":
                    prgSizeTextBox.Text = "80kb (5 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0081920";
                    break;
                case "06":
                    prgSizeTextBox.Text = "96kb (6 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0098304";
                    break;
                case "07":
                    prgSizeTextBox.Text = "112kb (7 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0114688";
                    break;
                case "08":
                    prgSizeTextBox.Text = "128kb (8 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0131072";
                    break;
                case "09":
                    prgSizeTextBox.Text = "144kb (9 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0147456";
                    break;
                case "0A":
                    prgSizeTextBox.Text = "160kb (10 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0163840";
                    break;
                case "0B":
                    prgSizeTextBox.Text = "176kb (11 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0180224";
                    break;
                case "0C":
                    prgSizeTextBox.Text = "192kb (12 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0196608";
                    break;
                case "0D":
                    prgSizeTextBox.Text = "208kb (13 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0212992";
                    break;
                case "0E":
                    prgSizeTextBox.Text = "224kb (14 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0229376";
                    break;
                case "0F":
                    prgSizeTextBox.Text = "240kb (15 x 16kb pages )";
                    prgSizeSplitTextBox.Text = "0245760";
                    break;
                case "10":
                    prgSizeTextBox.Text = "256kb (16 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0262144";
                    break;
                case "11":
                    prgSizeTextBox.Text = "272kb (17 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0278528";
                    break;
                case "12":
                    prgSizeTextBox.Text = "288kb (18 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0294912";
                    break;
                case "13":
                    prgSizeTextBox.Text = "304kb (19 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0311296";
                    break;
                case "14":
                    prgSizeTextBox.Text = "320kb (20 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0327680";
                    break;
                case "15":
                    prgSizeTextBox.Text = "336kb (21 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0344064";
                    break;
                case "16":
                    prgSizeTextBox.Text = "352kb (22 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0360448";
                    break;
                case "17":
                    prgSizeTextBox.Text = "368kb (23 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0376832";
                    break;
                case "18":
                    prgSizeTextBox.Text = "384kb (24 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0393216";
                    break;
                case "19":
                    prgSizeTextBox.Text = "400kb (25 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0409600";
                    break;
                case "1A":
                    prgSizeTextBox.Text = "416kb (26 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0425984";
                    break;
                case "1B":
                    prgSizeTextBox.Text = "432kb (27 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0442368";
                    break;
                case "1C":
                    prgSizeTextBox.Text = "448kb (28 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0458752";
                    break;
                case "1D":
                    prgSizeTextBox.Text = "464kb (29 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0475136";
                    break;
                case "1E":
                    prgSizeTextBox.Text = "480kb (30 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0491520";
                    break;
                case "1F":
                    prgSizeTextBox.Text = "496kb (31 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0507904";
                    break;
                case "20":
                    prgSizeTextBox.Text = "512kb (32 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0524288";
                    break;
                case "21":
                    prgSizeTextBox.Text = "528kb (33 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0540672";
                    break;
                case "22":
                    prgSizeTextBox.Text = "544kb (34 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0557056";
                    break;
                case "23":
                    prgSizeTextBox.Text = "560kb (35 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0573440";
                    break;
                case "24":
                    prgSizeTextBox.Text = "576kb (36 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0589824";
                    break;
                case "25":
                    prgSizeTextBox.Text = "592kb (37 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0606208";
                    break;
                case "26":
                    prgSizeTextBox.Text = "608kb (38 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0622592";
                    break;
                case "27":
                    prgSizeTextBox.Text = "624kb (39 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0638976";
                    break;
                case "28":
                    prgSizeTextBox.Text = "640kb (40 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0655360";
                    break;
                case "29":
                    prgSizeTextBox.Text = "656kb (41 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0671744";
                    break;
                case "2A":
                    prgSizeTextBox.Text = "672kb (42 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0688128";
                    break;
                case "2B":
                    prgSizeTextBox.Text = "688kb (43 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0704512";
                    break;
                case "2C":
                    prgSizeTextBox.Text = "704kb (44 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0720896";
                    break;
                case "2D":
                    prgSizeTextBox.Text = "720kb (45 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0737280";
                    break;
                case "2E":
                    prgSizeTextBox.Text = "736kb (46 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0753664";
                    break;
                case "2F":
                    prgSizeTextBox.Text = "752kb (47 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0770048";
                    break;
                case "30":
                    prgSizeTextBox.Text = "768kb (48 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0786432";
                    break;
                case "31":
                    prgSizeTextBox.Text = "784kb (49 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0802816";
                    break;
                case "32":
                    prgSizeTextBox.Text = "800kb (50 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0819200";
                    break;
                case "33":
                    prgSizeTextBox.Text = "816kb (51 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0835584";
                    break;
                case "34":
                    prgSizeTextBox.Text = "832kb (52 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0851968";
                    break;
                case "35":
                    prgSizeTextBox.Text = "848kb (53 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0868352";
                    break;
                case "36":
                    prgSizeTextBox.Text = "864kb (54 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0884736";
                    break;
                case "37":
                    prgSizeTextBox.Text = "880kb (55 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0901120";
                    break;
                case "38":
                    prgSizeTextBox.Text = "896kb (56 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0917504";
                    break;
                case "39":
                    prgSizeTextBox.Text = "912kb (57 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0933888";
                    break;
                case "3A":
                    prgSizeTextBox.Text = "928kb (58 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0950272";
                    break;
                case "3B":
                    prgSizeTextBox.Text = "944kb (59 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0966656";
                    break;
                case "3C":
                    prgSizeTextBox.Text = "960kb (60 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0983040";
                    break;
                case "3D":
                    prgSizeTextBox.Text = "976kb (61 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "0999424";
                    break;
                case "3E":
                    prgSizeTextBox.Text = "992kb (62 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "1015808";
                    break;
                case "3F":
                    prgSizeTextBox.Text = "1008kb (63 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "1032192";
                    break;
                case "40":
                    prgSizeTextBox.Text = "1024kb (64 x 16kb pages)";
                    prgSizeSplitTextBox.Text = "1048576";
                    break;
                default:
                    prgSizeTextBox.Text = "???kb (?? x ??kb pages)";
                    prgSizeSplitTextBox.Text = "0000000";
                    break;
            }
            #endregion

            #region CHR split info

            /*
             * 
                5 = CHR (Hex number depends on size of CHR file)

                [CHR - (Range 1 x 8kb pages -> 64 x 8kb pages)]
                -There is NO MINIMUM required for CHR data because some games have the CHR
                 data imbedded into the PRG info.

                * NOTE: To figure out the exact size in bytes each of these pages are worth
                        just start at 1 x 8kb pages (aka: 8192 bytes) and just keep adding
                        8192 bytes more for each "page" higher.

                (1 x 8kb pages)  = 01       (2 x 8kb pages)  = 02       (3 x 8kb pages)  = 03
                (4 x 8kb pages)  = 04       (5 x 8kb pages)  = 05       (6 x 8kb pages)  = 06
                (7 x 8kb pages)  = 07       (8 x 8kb pages)  = 08       (9 x 8kb pages)  = 09
                (10 x 8kb pages) = 0A       (11 x 8kb pages) = 0B       (12 x 8kb pages) = 0C
                (13 x 8kb pages) = 0D       (14 x 8kb pages) = 0E       (15 x 8kb pages) = 0F
                (16 x 8kb pages) = 10       (17 x 8kb pages) = 11       (18 x 8kb pages) = 12
                (19 x 8kb pages) = 13       (20 x 8kb pages) = 14       (21 x 8kb pages) = 15
                (22 x 8kb pages) = 16       (23 x 8kb pages) = 17       (24 x 8kb pages) = 18
                (25 x 8kb pages) = 19       (26 x 8kb pages) = 1A       (27 x 8kb pages) = 1B
                (28 x 8kb pages) = 1C       (29 x 8kb pages) = 1D       (30 x 8kb pages) = 1E
                (31 x 8kb pages) = 1F       (32 x 8kb pages) = 20       (33 x 8kb pages) = 21
                (34 x 8kb pages) = 22       (35 x 8kb pages) = 23       (36 x 8kb pages) = 24
                (37 x 8kb pages) = 25       (38 x 8kb pages) = 26       (39 x 8kb pages) = 27
                (40 x 8kb pages) = 28       (41 x 8kb pages) = 29       (42 x 8kb pages) = 2A
                (43 x 8kb pages) = 2B       (44 x 8kb pages) = 2C       (45 x 8kb pages) = 2D
                (46 x 8kb pages) = 2E       (47 x 8kb pages) = 2F       (48 x 8kb pages) = 30
                (49 x 8kb pages) = 31       (50 x 8kb pages) = 32       (51 x 8kb pages) = 33
                (52 x 8kb pages) = 34       (53 x 8kb pages) = 35       (54 x 8kb pages) = 36
                (55 x 8kb pages) = 37       (56 x 8kb pages) = 38       (57 x 8kb pages) = 39
                (58 x 8kb pages) = 3A       (59 x 8kb pages) = 3B       (60 x 8kb pages) = 3C
                (61 x 8kb pages) = 3D       (62 x 8kb pages) = 3E       (63 x 8kb pages) = 3F
                (64 x 8kb pages) = 40
             */
            
            chrSizeString = upperNibbleByteFiveString + lowerNibbleByteFiveString;

            switch (chrSizeString)
            {
                case "00":
                    chrSizeTextBox.Text = "0kb (0 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0000000";
                    break;
                case "01":
                    chrSizeTextBox.Text = "8kb (1 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0008192";
                    break;
                case "02":
                    chrSizeTextBox.Text = "16kb (2 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0016384";
                    break;
                case "03":
                    chrSizeTextBox.Text = "24kb (3 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0024576";
                    break;
                case "04":
                    chrSizeTextBox.Text = "32kb (4 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0032768";
                    break;
                case "05":
                    chrSizeTextBox.Text = "40kb (5 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0040960";
                    break;
                case "06":
                    chrSizeTextBox.Text = "48kb (6 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0049152";
                    break;
                case "07":
                    chrSizeTextBox.Text = "56kb (7 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0057344";
                    break;
                case "08":
                    chrSizeTextBox.Text = "64kb (8 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0065536";
                    break;
                case "09":
                    chrSizeTextBox.Text = "72kb (9 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0073728";
                    break;
                case "0A":
                    chrSizeTextBox.Text = "80kb (10 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0081920";
                    break;
                case "0B":
                    chrSizeTextBox.Text = "88kb (11 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0090112";
                    break;
                case "0C":
                    chrSizeTextBox.Text = "96kb (12 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0098304";
                    break;
                case "0D":
                    chrSizeTextBox.Text = "104kb (13 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0106496";
                    break;
                case "0E":
                    chrSizeTextBox.Text = "112kb (14 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0114688";
                    break;
                case "0F":
                    chrSizeTextBox.Text = "120kb (15 x 8kb pages )";
                    chrSizeSplitTextBox.Text = "0122880";
                    break;
                case "10":
                    chrSizeTextBox.Text = "128kb (16 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0131072";
                    break;
                case "11":
                    chrSizeTextBox.Text = "136kb (17 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0139264";
                    break;
                case "12":
                    chrSizeTextBox.Text = "144kb (18 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0147456";
                    break;
                case "13":
                    chrSizeTextBox.Text = "152kb (19 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "155648";
                    break;
                case "14":
                    chrSizeTextBox.Text = "160kb (20 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0163840";
                    break;
                case "15":
                    chrSizeTextBox.Text = "168kb (21 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0172032";
                    break;
                case "16":
                    chrSizeTextBox.Text = "176kb (22 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0180224";
                    break;
                case "17":
                    chrSizeTextBox.Text = "184kb (23 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0188416";
                    break;
                case "18":
                    chrSizeTextBox.Text = "192kb (24 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0196608";
                    break;
                case "19":
                    chrSizeTextBox.Text = "200kb (25 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0204800";
                    break;
                case "1A":
                    chrSizeTextBox.Text = "208kb (26 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0212992";
                    break;
                case "1B":
                    chrSizeTextBox.Text = "216kb (27 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0221184";
                    break;
                case "1C":
                    chrSizeTextBox.Text = "224kb (28 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0229376";
                    break;
                case "1D":
                    chrSizeTextBox.Text = "232kb (29 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0237568";
                    break;
                case "1E":
                    chrSizeTextBox.Text = "240kb (30 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0245760";
                    break;
                case "1F":
                    chrSizeTextBox.Text = "248kb (31 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0253952";
                    break;
                case "20":
                    chrSizeTextBox.Text = "256kb (32 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0262144";
                    break;
                case "21":
                    chrSizeTextBox.Text = "264kb (33 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0270336";
                    break;
                case "22":
                    chrSizeTextBox.Text = "272kb (34 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0278528";
                    break;
                case "23":
                    chrSizeTextBox.Text = "280kb (35 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0286720";
                    break;
                case "24":
                    chrSizeTextBox.Text = "288kb (36 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0294912";
                    break;
                case "25":
                    chrSizeTextBox.Text = "296kb (37 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0303104";
                    break;
                case "26":
                    chrSizeTextBox.Text = "304kb (38 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0311296";
                    break;
                case "27":
                    chrSizeTextBox.Text = "312kb (39 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0319488";
                    break;
                case "28":
                    chrSizeTextBox.Text = "320kb (40 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0327680";
                    break;
                case "29":
                    chrSizeTextBox.Text = "328kb (41 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0335872";
                    break;
                case "2A":
                    chrSizeTextBox.Text = "336kb (42 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0344064";
                    break;
                case "2B":
                    chrSizeTextBox.Text = "344kb (43 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0352256";
                    break;
                case "2C":
                    chrSizeTextBox.Text = "352kb (44 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0360448";
                    break;
                case "2D":
                    chrSizeTextBox.Text = "360kb (45 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0368640";
                    break;
                case "2E":
                    chrSizeTextBox.Text = "368kb (46 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0376832";
                    break;
                case "2F":
                    chrSizeTextBox.Text = "376kb (47 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0385024";
                    break;
                case "30":
                    chrSizeTextBox.Text = "384kb (48 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0393216";
                    break;
                case "31":
                    chrSizeTextBox.Text = "392kb (49 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0401408";
                    break;
                case "32":
                    chrSizeTextBox.Text = "400kb (50 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0409600";
                    break;
                case "33":
                    chrSizeTextBox.Text = "408kb (51 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0417792";
                    break;
                case "34":
                    chrSizeTextBox.Text = "416kb (52 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0425984";
                    break;
                case "35":
                    chrSizeTextBox.Text = "424kb (53 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0434176";
                    break;
                case "36":
                    chrSizeTextBox.Text = "432kb (54 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0442368";
                    break;
                case "37":
                    chrSizeTextBox.Text = "440kb (55 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0450560";
                    break;
                case "38":
                    chrSizeTextBox.Text = "448kb (56 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0458752";
                    break;
                case "39":
                    chrSizeTextBox.Text = "456kb (57 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0466944";
                    break;
                case "3A":
                    chrSizeTextBox.Text = "464kb (58 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0475136";
                    break;
                case "3B":
                    chrSizeTextBox.Text = "472kb (59 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0483328";
                    break;
                case "3C":
                    chrSizeTextBox.Text = "480kb (60 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0491520";
                    break;
                case "3D":
                    chrSizeTextBox.Text = "488kb (61 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0499712";
                    break;
                case "3E":
                    chrSizeTextBox.Text = "496kb (62 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0507904";
                    break;
                case "3F":
                    chrSizeTextBox.Text = "504kb (63 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0516096";
                    break;
                case "40":
                    chrSizeTextBox.Text = "512kb (64 x 8kb pages)";
                    chrSizeSplitTextBox.Text = "0524288";
                    break;
                default:
                    chrSizeTextBox.Text = "???kb (?? x ??kb pages)";
                    chrSizeSplitTextBox.Text = "0000000";
                    break;
            }
            #endregion

            #region Byte 6 and 7 - mappers
            /*
             * Byte 6:
             * 7       0
             * ---------
             * NNNN FTBM
             * Bit 7: N: Lower 4 bits of the mapper number
             * Bit 6: N: Lower 4 bits of the mapper number
             * Bit 5: N: Lower 4 bits of the mapper number
             * Bit 4: N: Lower 4 bits of the mapper number
             * Bit 3: F: Four screen mode. 0 = no, 1 = yes. (When set, the M bit has no effect)
             * Bit 2: T: Trainer.  0 = no trainer present, 1 = 512 byte trainer at 7000-71FFh
             * Bit 1: B: SRAM at 6000-7FFFh battery backed.  0= no, 1 = yes
             * Bit 0: M: Mirroring.  0 = horizontal, 1 = vertical.
             * 
             * Byte 7:
             * 7       0
             * ---------
             * NNNN SSPV
             * Bit 7: N: Upper 4 bits of the mapper number
             * Bit 6: N: Upper 4 bits of the mapper number
             * Bit 5: N: Upper 4 bits of the mapper number
             * Bit 4: N: Upper 4 bits of the mapper number
             * Bit 3: S: these bits are not used in iNES. <- NOTE: When equal to binary 10, 
             * Bit 2: S: these bits are not used in iNES. <- use NES 2.0 rules; otherwise, use other rules.
             * Bit 1: P: Playchoice 10.  When set, this is a PC-10 game
             * Bit 0: V: Vs. Unisystem.  When set, this is a Vs. game
             */

            mapperHexString = upperNibbleByteSevenString + upperNibbleByteSixString;
            mapperInt = int.Parse(mapperHexString, System.Globalization.NumberStyles.HexNumber);
            mapperDecString = mapperInt.ToString();

            exampleRomsTextBox.Text = "";

            switch (mapperInt)
            {
                case 0:
                    mapperTextBox.Text = "No Mapper - NROM, or unknown mapper]";
                    exampleRomsTextBox.Text = "Ice Climber, Excitebike, Balloon Fight, Super Mario Bros";
                    break;
                case 1:
                    mapperTextBox.Text = "1 - Nintendo MMC1 Chipset / MMC1 - S(x)ROM";
                    exampleRomsTextBox.Text = "Final Fantasy, Mega Man 2, Blaster Master, Metroid, Kid Icarus, Zelda, Zelda 2, Castlevania 2";
                    break;
                case 2:
                    mapperTextBox.Text = "2 - ROM (PRG) Switch / U(x)ROM";
                    exampleRomsTextBox.Text = "Mega Man, Castlevania, Contra, Duck Tales, Metal Gear";
                    break;
                case 3:
                    mapperTextBox.Text = "3 - VROM (CHR) Switch / CNROM";
                    exampleRomsTextBox.Text = "Solomon's Key, Arkanoid, Arkista's Ring, Bump 'n' Jump, Cybernoid";
                    break;
                case 4:
                    mapperTextBox.Text = "4 - Nintendo MMC3 Chipset / MMC3 - T(x)ROM / MMC6 - H(x)ROM";
                    exampleRomsTextBox.Text = "Mega Man 3, 4, 5, 6, Kirby's Adventure, Gauntlet, Rad Racer 2, Startropics 1, 2 (MMC6), Super Mario Bros. 2, 3";
                    break;
                case 5:
                    mapperTextBox.Text = "5 - Nintendo MMC5 Chipset / MMC5 - E(x)ROM";
                    exampleRomsTextBox.Text = "Castlevania 3, Just Breed, Uncharted Waters, Romance of the 3 Kingdoms 2, Laser Invasion, Metal Slader Glory, Uchuu Keibitai SDF, Shin 4 Nin Uchi Mahjong - Yakuman Tengoku";
                    break;
                case 6:
                    mapperTextBox.Text = "6 - FFE F4XXX Games";
                    break;
                case 7:
                    mapperTextBox.Text = "7 - 32kb ROM (PRG) Switch - A(x)ROM";
                    exampleRomsTextBox.Text = "Battletoads, Time Lord, Marble Madness";
                    break;
                case 8:
                    mapperTextBox.Text = "8 - FFE F3XXX Games";
                    break;
                case 9:
                    mapperTextBox.Text = "9 - Nintendo MMC2 Chipset / MMC2 - P(x)ROM";
                    exampleRomsTextBox.Text = "Mike Tyson's Punch Out!!";
                    break;
                case 10:
                    mapperTextBox.Text = "10 - Nintendo MMC4 Chipset / MMC4";
                    exampleRomsTextBox.Text = "Fire Emblem";
                    break;
                case 11:
                    mapperTextBox.Text = "11 - Color Dreams Chipset";
                    exampleRomsTextBox.Text = "Crystal Mines, Metal Fighter";
                    break;
                case 12:
                    mapperTextBox.Text = "12 - FFE F6XXX Games / DBZ5 (MMC3 Variant)";
                    break;
                case 13:
                    mapperTextBox.Text = "13 - CPROM";
                    exampleRomsTextBox.Text = "Videomation";
                    break;
                case 15:
                    mapperTextBox.Text = "15 - 100-in-1 Cart Switch / Multicart";
                    exampleRomsTextBox.Text = "100-in-1 Contra Function 16";
                    break;
                case 16:
                    mapperTextBox.Text = "16 - Bandai Chipset";
                    exampleRomsTextBox.Text = "Dragon Ball - Dai Maou Jukkatsu, Dragon Ball Z Gaiden, Dragon Ball Z 2, Rokudenashi Blues, Akuma-kun - Makai no Wana";
                    break;
                case 17:
                    mapperTextBox.Text = "17 - FFE F8XXX Games";
                    break;
                case 18:
                    mapperTextBox.Text = "18 - Jaleco SS8806 Chipset";
                    exampleRomsTextBox.Text = "The Lord of King, Magic John, Pizza Pop";
                    break;
                case 19:
                    mapperTextBox.Text = "19 - Namcot 106 Chipset / N106";
                    exampleRomsTextBox.Text = "Digital Devil Story - Megami Tensei 2, Final Lap, Rolling Thunder (J), Splatter House, Mappy Kids";
                    break;
                case 20:
                    mapperTextBox.Text = "20 - Famicom Disk System";
                    break;
                case 21:
                    mapperTextBox.Text = "21 - Konami VRC4 2a Chipset";
                    exampleRomsTextBox.Text = "Wai Wai World 2, Ganbare Goemon Gaiden 2";
                    break;
                case 22:
                    mapperTextBox.Text = "22 - Konami VRC4 1b Chipset";
                    exampleRomsTextBox.Text = "Ganbare Pennant Race, TwinBee 3";
                    break;
                case 23:
                    mapperTextBox.Text = "23 - Konami VRC4 1a Chipset";
                    exampleRomsTextBox.Text = "Wai Wai World";
                    break;
                case 24:
                    mapperTextBox.Text = "24 - Konami VRC6 Chipset";
                    exampleRomsTextBox.Text = "Akumajou Densetsu";
                    break;
                case 25:
                    mapperTextBox.Text = "25 - Konami VRC4 Chipset";
                    break;
                case 26:
                    mapperTextBox.Text = "26 - VRC6 Variant (newer) Chipset";
                    exampleRomsTextBox.Text = "Madara, Esper Dream 2";
                    break;
                case 27:
                    mapperTextBox.Text = "27 - World Hero";
                    break;
                case 32:
                    mapperTextBox.Text = "32 - Irem G-101 Chipset";
                    exampleRomsTextBox.Text = "Image Fight, Major League, Kaiketsu Yanchamaru 2";
                    break;
                case 33:
                    mapperTextBox.Text = "33 - Taito TC0190/TC0350";
                    exampleRomsTextBox.Text = "Akira, Bakushou!! Jinsei Gekijou, Don Doko Don, Insector X";
                    break;
                case 34:
                    mapperTextBox.Text = "34 - 32kb ROM (PRG) Switch B(x)ROM or NINA-001";
                    exampleRomsTextBox.Text = "Darkseed (BxROM), Mashou(BxROM), Impossible Mission 2(NINA-001)";
                    break;
                case 36:
                    mapperTextBox.Text = "36 - Strike Wolf";
                    break;
                case 37:
                    mapperTextBox.Text = "37 - MMC3 Multicart";
                    break;
                case 38:
                    mapperTextBox.Text = "38 - Brazil";
                    break;
                case 39:
                    mapperTextBox.Text = "39 - Subor";
                    break;
                case 40:
                    mapperTextBox.Text = "40 - Pirate";
                    break;
                case 41:
                    mapperTextBox.Text = "41 - Caltron Multicart";
                    break;
                case 42:
                    mapperTextBox.Text = "42 - Pirate";
                    break;
                case 43:
                    mapperTextBox.Text = "43 - Pirate Multicart";
                    break;
                case 44:
                    mapperTextBox.Text = "44 - MMC3 Based / Multicart";
                    exampleRomsTextBox.Text = "Super Big 7-in-1";
                    break;
                case 45:
                    mapperTextBox.Text = "45 - Super (X)-in-1 MMC3 Based / Multicart";
                    exampleRomsTextBox.Text = "Super 8-in-1, Super 4-in-1, Super 1000000-in-1";
                    break;
                case 46:
                    mapperTextBox.Text = "46 - Multicart";
                    exampleRomsTextBox.Text = "Rumblestation 15-in-1";
                    break;
                case 47:
                    mapperTextBox.Text = "47 - Multicart - MMC3 Based";
                    exampleRomsTextBox.Text = "Super Spike V'Ball + Nintendo World Cup";
                    break;
                case 48:
                    mapperTextBox.Text = "48 - MMC3 Variant";
                    exampleRomsTextBox.Text = "Bubble Bobble 2 (J), Don Doko Don 2, Captain Saver";
                    break;
                case 49:
                    mapperTextBox.Text = "49 - Multicart - MMC3 Based";
                    exampleRomsTextBox.Text = "Super HIK 4-in-1";
                    break;
                case 50:
                    mapperTextBox.Text = "50 - Pirate";
                    exampleRomsTextBox.Text = "Super Mario Bros. (JU) (Alt Levels), (SMB2j pirate cart)";
                    break;
                case 51:
                    mapperTextBox.Text = "51 - Pirate Multicart";
                    exampleRomsTextBox.Text = "Mario 7-in-1";
                    break;
                case 52:
                    mapperTextBox.Text = "52 - Multicart - MMC3 Based";
                    exampleRomsTextBox.Text = "Mario 7-in-1";
                    break;
                case 53:
                    mapperTextBox.Text = "53 - Pirate Multicart";
                    break;
                case 54:
                    mapperTextBox.Text = "54 - Multicart";
                    break;
                case 55:
                    mapperTextBox.Text = "55 - Pirate";
                    break;
                case 56:
                    mapperTextBox.Text = "56 - Pirate";
                    break;
                case 57:
                    mapperTextBox.Text = "57 - Multicart";
                    exampleRomsTextBox.Text = "GK 47-in-1, 6-in-1 (SuperGK)";
                    break;
                case 58:
                    mapperTextBox.Text = "58 - Multicart";
                    exampleRomsTextBox.Text = "68-in-1 (Game Star), Study and Game 32-in-1";
                    break;
                case 60:
                    mapperTextBox.Text = "60 - Reset Based 4-in-1 / NROM / Multicart";
                    exampleRomsTextBox.Text = "Reset Based 4-in-1";
                    break;
                case 61:
                    mapperTextBox.Text = "61 - Multicart";
                    exampleRomsTextBox.Text = "20-in-1";
                    break;
                case 62:
                    mapperTextBox.Text = "62 - Multicart";
                    exampleRomsTextBox.Text = "Super 700-in-1";
                    break;
                case 64:
                    mapperTextBox.Text = "64 - Tengen Rambo-1";
                    exampleRomsTextBox.Text = "Klax, Skull and Crossbones, Shinobi";
                    break;
                case 65:
                    mapperTextBox.Text = "65 - Irem H3001 Chipset / Misc (J)";
                    exampleRomsTextBox.Text = "Daiku no Gen San 2, Kaiketsu Yanchamaru 3, Spartan X 2";
                    break;
                case 66:
                    mapperTextBox.Text = "66 - 74161/32 Chipset - G(x)ROM";
                    exampleRomsTextBox.Text = "Doraemon, Dragon Power, Gumshoe, Thunder & Lightning, Super Mario Bros. / Duck Hunt";
                    break;
                case 67:
                    mapperTextBox.Text = "67 - Sunsoft-3";
                    exampleRomsTextBox.Text = "Fantasy Zone 2 (J), Mito Koumon - Sekai Manyuu Ki";
                    break;
                case 68:
                    mapperTextBox.Text = "68 - Sunsoft-4";
                    exampleRomsTextBox.Text = "After Burner 2, Maharaja";
                    break;
                case 69:
                    mapperTextBox.Text = "69 - Sunsoft Mapper 4, FME-7, Sunsoft 5B";
                    exampleRomsTextBox.Text = "Gimmick!, Batman:  Return of the Joker, Hebereke, Gremlins 2 (J)";
                    break;
                case 70:
                    mapperTextBox.Text = "70 - 74161/32 Chipset";
                    exampleRomsTextBox.Text = "Family Trainer - Manhattan Police, Family Trainer - Meiro Daisakusen, Kamen Rider Club, Space Shadow";
                    break;
                case 71:
                    mapperTextBox.Text = "71 - Camerica";
                    exampleRomsTextBox.Text = "MiG 29 - Soviet Fighter, Fire Hawk, The Fantastic Adventures of Dizzy, Bee 52";
                    break;
                case 72:
                    mapperTextBox.Text = "72 - Jaleco Early Mapper / Misc (J)";
                    exampleRomsTextBox.Text = "Pinball Quest (J), Moero!! Pro Tennis, Moero!! Juudou Warriors";
                    break;
                case 73:
                    mapperTextBox.Text = "73 - VRC3 - Konami VRC";
                    exampleRomsTextBox.Text = "Salamander";
                    break;
                case 74:
                    mapperTextBox.Text = "74 - Pirate MMC3 variant - Taiwan MMC3 / Pirate (CN)";
                    exampleRomsTextBox.Text = "Di 4 Ci - Ji Qi Ren Dai Zhan, Ji Jia Zhan Shi";
                    break;
                case 75:
                    mapperTextBox.Text = "75 - Jaleco Mapper SS8805 / VRC1";
                    exampleRomsTextBox.Text = "Tetsuwan Atom, Ganbare Goemon! - Karakuri Douchuu";
                    break;
                case 76:
                    mapperTextBox.Text = "76 - Namco 109";
                    exampleRomsTextBox.Text = "Digital Devil Story - Megami Tensei";
                    break;
                case 77:
                    mapperTextBox.Text = "77 - Irem Early Mapper 0";
                    exampleRomsTextBox.Text = "Napoleon Senki";
                    break;
                case 78:
                    mapperTextBox.Text = "78 - 74161/32";
                    exampleRomsTextBox.Text = "Holy Diver, Uchuusen - Cosmo Carrier";
                    break;
                case 79:
                    mapperTextBox.Text = "79 - American Video Ent. / NINA-06";
                    exampleRomsTextBox.Text = "Blackjack, Dudes with Attitude, F-15 City War, Krazy Kreatures";
                    break;
                case 80:
                    mapperTextBox.Text = "80 - X-005 Chipset";
                    exampleRomsTextBox.Text = "Kyonshiizu 2, Minelvaton Saga, Taito Grand Prix - Eikou heno License";
                    break;
                case 81:
                    mapperTextBox.Text = "81 - C075 Chipset";
                    break;
                case 82:
                    mapperTextBox.Text = "82 - X1-17 Chipset";
                    exampleRomsTextBox.Text = "SD Keiji - Blader, Kyuukyoku Harikiri Stadium";
                    break;
                case 83:
                    mapperTextBox.Text = "83 - Cony Mapper / Pirate";
                    break;
                case 84:
                    mapperTextBox.Text = "84 - PasoFami Mapper!";
                    break;
                case 85:
                    mapperTextBox.Text = "85 - Konami VRC7";
                    exampleRomsTextBox.Text = "Lagrange Point, Tiny Toon Adventures 2 (J)";
                    break;
                case 86:
                    mapperTextBox.Text = "86 - Misc (J)";
                    exampleRomsTextBox.Text = "Moero!! Pro Yakyuu (Black), Moero!! Pro Yakyuu (Red)";
                    break;
                case 87:
                    mapperTextBox.Text = "87 - Misc (J)";
                    exampleRomsTextBox.Text = "Argus (J), City Connection (J), Ninja Jajamaru Kun";
                    break;
                case 88:
                    mapperTextBox.Text = "88 - Misc (J)";
                    exampleRomsTextBox.Text = "Quinty (J), Namcot Mahjong 3, Dragon Spirit - Aratanaru Densetsu";
                    break;
                case 89:
                    mapperTextBox.Text = "89 - Sunsoft-2";
                    exampleRomsTextBox.Text = "Mito Koumon";
                    break;
                case 90:
                    mapperTextBox.Text = "90 - Pirate";
                    exampleRomsTextBox.Text = "Tekken 2, Mortal Kombat 2, Super Contra 3, Super Mario World";
                    break;
                case 91:
                    mapperTextBox.Text = "91 - Pirate";
                    exampleRomsTextBox.Text = "Street Fighter 3";
                    break;
                case 92:
                    mapperTextBox.Text = "92 - Misc (J)";
                    exampleRomsTextBox.Text = "Moero!! Pro Soccer, Moero!! Pro Yakyuu '88 - Ketteiban";
                    break;
                case 93:
                    mapperTextBox.Text = "93 - Sunsoft-2";
                    exampleRomsTextBox.Text = "Fantasy Zone (J)";
                    break;
                case 94:
                    mapperTextBox.Text = "94 - Misc (J)";
                    exampleRomsTextBox.Text = "Senjou no Ookami";
                    break;
                case 95:
                    mapperTextBox.Text = "95 - MMC3 variant";
                    exampleRomsTextBox.Text = "Dragon Buster (J)";
                    break;
                case 96:
                    mapperTextBox.Text = "96 - Misc (J)";
                    exampleRomsTextBox.Text = "Oeka Kids - Anpanman no Hiragana Daisuki, Oeka Kids - Anpanman to Oekaki Shiyou!!";
                    break;
                case 97:
                    mapperTextBox.Text = "97 - Misc (J)";
                    exampleRomsTextBox.Text = "Kaiketsu Yanchamaru";
                    break;
                case 99:
                    mapperTextBox.Text = "99 - VS (Arcade)";
                    break;
                case 100:
                    mapperTextBox.Text = "100 - Nestice - Buggy Mode";
                    break;
                case 101:
                    mapperTextBox.Text = "101 - Junk";
                    break;
                case 103:
                    mapperTextBox.Text = "103 - FDS Conversion";
                    break;
                case 104:
                    mapperTextBox.Text = "104 - Camerica";
                    break;
                case 105:
                    mapperTextBox.Text = "105 - NES-EVENT";
                    exampleRomsTextBox.Text = "Nintendo World Championships 1990";
                    break;
                case 106:
                    mapperTextBox.Text = "106 - Pirate";
                    break;
                case 107:
                    mapperTextBox.Text = "107 - Unlicensed";
                    exampleRomsTextBox.Text = "Magic Dragon";
                    break;
                case 108:
                    mapperTextBox.Text = "108 - FDS Conversion";
                    break;
                case 111:
                    mapperTextBox.Text = "111 - Misc (CN)";
                    break;
                case 112:
                    mapperTextBox.Text = "112 - Misc (CN)";
                    exampleRomsTextBox.Text = "Huang Di San Guo Zhi - Qun Xiong Zheng Ba";
                    break;
                case 113:
                    mapperTextBox.Text = "113 - Mislabeled Nina_006";
                    exampleRomsTextBox.Text = "Rad Racket - Deluxe Tennis II Papillion";
                    break;
                case 115:
                    mapperTextBox.Text = "115 - MMC3 variant";
                    exampleRomsTextBox.Text = "Yuu Yuu Hakusho Final - Makai Saikyou Retsuden";
                    break;
                case 116:
                    mapperTextBox.Text = "116 - MMC3 variant";
                    break;
                case 117:
                    mapperTextBox.Text = "117 - Chinese";
                    break;
                case 118:
                    mapperTextBox.Text = "118 - MMC3 variant / TLSROM";
                    exampleRomsTextBox.Text = "Armadillo, Pro Sport Hockey";
                    break;
                case 119:
                    mapperTextBox.Text = "119 - MMC3 variant / TQROM";
                    exampleRomsTextBox.Text = "High Speed, Pinbot";
                    break;
                case 120:
                    mapperTextBox.Text = "120 - FDS Conversion";
                    break;
                case 121:
                    mapperTextBox.Text = "121 - Pirate";
                    break;
                case 123:
                    mapperTextBox.Text = "123 - Pirate";
                    break;
                case 125:
                    mapperTextBox.Text = "124 - Pirate";
                    break;
                case 132:
                    mapperTextBox.Text = "132 - Misc";
                    break;
                case 133:
                    mapperTextBox.Text = "133 - Sachen";
                    break;
                case 134:
                    mapperTextBox.Text = "134 - Misc (CN)";
                    break;
                case 136:
                case 137:
                case 138:
                case 139:
                    mapperTextBox.Text = mapperInt + " - Sachen";
                    break;
                case 140:
                    mapperTextBox.Text = "140 - Misc (J)";
                    exampleRomsTextBox.Text = "Bio Senshi Dan - Increaser Tono Tatakai";
                    break;
                case 141:
                case 142:
                case 143:
                    mapperTextBox.Text = mapperInt + " - Sachen";
                    break;
                case 144:
                    mapperTextBox.Text = "144 - Variant of Mapper 11 - Color Dreams Chipset";
                    break;
                case 145:
                case 146:
                case 147:
                case 148:
                case 149:
                case 150:
                    mapperTextBox.Text = mapperInt + " - Sachen";
                    break;
                case 151:
                    mapperTextBox.Text = "151 - Vs. Unisystem";
                    break;
                case 152:
                    mapperTextBox.Text = "152 - Misc (J)";
                    exampleRomsTextBox.Text = "Arkanoid 2 (J), Gegege no Kitarou 2";
                    break;
                case 153:
                    mapperTextBox.Text = "153 - Bandai";
                    break;
                case 154:
                    mapperTextBox.Text = "154 - Misc (J)";
                    exampleRomsTextBox.Text = "Devil Man";
                    break;
                case 155:
                    mapperTextBox.Text = "155 - MMC1 Clone";
                    break;
                case 156:
                    mapperTextBox.Text = "156 - Korean";
                    break;
                case 157:
                    mapperTextBox.Text = "157 - Bandai";
                    break;
                case 158:
                    mapperTextBox.Text = "158 - Tengen";
                    break;
                case 159:
                    mapperTextBox.Text = "159 - Clone of Mapper 16 - Bandai Chipset";
                    exampleRomsTextBox.Text = "Dragon Ball Z - Kyoushuu! Saiya Jin, SD Gundam Gaiden, Magical Taruruuto Kun 1, 2";
                    break;
                case 160:
                    mapperTextBox.Text = "160 - Sachen";
                    break;
                case 162:
                case 163:
                    mapperTextBox.Text = mapperInt + " - Chinese";
                    break;
                case 164:
                    mapperTextBox.Text = "164 - Pirate";
                    exampleRomsTextBox.Text = "Final Fantasy V";
                    break;
                case 165:
                    mapperTextBox.Text = "165 - MMC3 Variant";
                    exampleRomsTextBox.Text = "Fire Emblem (Unl)";
                    break;
                case 166:
                case 167:
                    mapperTextBox.Text = mapperInt + " - Subor";
                    break;
                case 168:
                    mapperTextBox.Text = "168 - Racemate Challenger II";
                    break;
                case 169:
                    mapperTextBox.Text = "169 - Pirate Multicart";
                    break;
                case 170:
                    mapperTextBox.Text = "170 - Shiko Game Syu";
                    break;
                case 171:
                    mapperTextBox.Text = "171 - Tui Do Woo Ma Jeung";
                    break;
                case 174:
                    mapperTextBox.Text = "174 - Multicart";
                    break;
                case 175:
                    mapperTextBox.Text = "175 - Pirate Multicart";
                    break;
                case 176:
                    mapperTextBox.Text = "176 - Chinese WXN";
                    break;
                case 177:
                    mapperTextBox.Text = "177 - Chinese";
                    break;
                case 178:
                    mapperTextBox.Text = "178 - Chinese WXN";
                    break;
                case 180:
                    mapperTextBox.Text = "180 - Misc (J)";
                    exampleRomsTextBox.Text = "Crazy Climber (J)";
                    break;
                case 182:
                    mapperTextBox.Text = "182 - MMC3 Variant / Scrambled";
                    exampleRomsTextBox.Text = "Pocahontas, Super Donkey Kong";
                    break;
                case 183:
                    mapperTextBox.Text = "183 - Shui Guan Pipe";
                    break;
                case 184:
                    mapperTextBox.Text = "184 - Sunsoft-1";
                    exampleRomsTextBox.Text = "Atlantis no Nazo, The Wing of Madoola";
                    break;
                case 185:
                    mapperTextBox.Text = "185 - Misc (J)";
                    exampleRomsTextBox.Text = "Spy Vs. Spy (J), Mighty Bomb Jack (J)";
                    break;
                case 186:
                    mapperTextBox.Text = "186 - Study Box (J)";
                    break;
                case 187:
                    mapperTextBox.Text = "187 - Pirate";
                    break;
                case 188:
                    mapperTextBox.Text = "188 - Karaoke (J)";
                    break;
                case 189:
                    mapperTextBox.Text = "189 - MMC3 variant";
                    exampleRomsTextBox.Text = "Thunder Warrior";
                    break;
                case 191:
                    mapperTextBox.Text = "191 - Pirate / MMC3 variant";
                    exampleRomsTextBox.Text = "Sugoro Quest - Dice no Senshitachi (As)";
                    break;
                case 192:
                    mapperTextBox.Text = "192 - Chinese WXN / Pirate / MMC3 variant";
                    exampleRomsTextBox.Text = "Ying Lie Qun Xia Zhuan";
                    break;
                case 193:
                    mapperTextBox.Text = "193 - Unlicensed";
                    exampleRomsTextBox.Text = "Fighting Hero (Unl)";
                    break;
                case 194:
                    mapperTextBox.Text = "194 - Chinese WXN / Pirate / MMC3 variant";
                    exampleRomsTextBox.Text = "Dai-2-Ji - Super Robot Taisen (As)";
                    break;
                case 195:
                    mapperTextBox.Text = "195 - Chinese WXN";
                    break;
                case 196:
                    mapperTextBox.Text = "196 - Pirate";
                    break;
                case 197:
                    mapperTextBox.Text = "197 - Street Fighter III";
                    exampleRomsTextBox.Text = "Street Fighter III";
                    break;
                case 198:
                case 199:
                    mapperTextBox.Text = mapperInt + " - Pirate";
                    break;
                case 200:
                    mapperTextBox.Text = "200 - Multicart";
                    exampleRomsTextBox.Text = "1200-in-1, 36-in-1";
                    break;
                case 201:
                    mapperTextBox.Text = "201 - Multicart";
                    exampleRomsTextBox.Text = "8-in-1, 21-in-1 (2006-CA) (Unl)";
                    break;
                case 202:
                    mapperTextBox.Text = "202 - Pirate";
                    break;
                case 203:
                    mapperTextBox.Text = "203 - Multicart";
                    exampleRomsTextBox.Text = "35-in-1";
                    break;
                case 204:
                    mapperTextBox.Text = "204 - Pirate";
                    break;
                case 205:
                    mapperTextBox.Text = "205 - Multicart";
                    exampleRomsTextBox.Text = "15-in-1, 3-in-1";
                    break;
                case 206:
                    mapperTextBox.Text = "206 - Namcot109";
                    break;
                case 207:
                    mapperTextBox.Text = "207 - Misc (J)";
                    exampleRomsTextBox.Text = "Fudou Myouou Den";
                    break;
                case 209:
                    mapperTextBox.Text = "209 - Garbage";
                    exampleRomsTextBox.Text = "Shin Samurai Spirits 2";
                    break;
                case 210:
                    mapperTextBox.Text = "210 - Namcot";
                    exampleRomsTextBox.Text = "Family Circuit '91, Wagyan Land 2,3, Dream Master";
                    break;
                case 211:
                    mapperTextBox.Text = "211 - Pirate";
                    break;
                case 212:
                case 213:
                    mapperTextBox.Text = mapperInt + " - Ten Million Games in One";
                    exampleRomsTextBox.Text = "Ten Million Games in One";
                    break;
                case 214:
                    mapperTextBox.Text = "214 - Multicart";
                    break;
                case 215:
                    mapperTextBox.Text = "215 - Pirate";
                    break;
                case 216:
                case 217:
                    mapperTextBox.Text = mapperInt + " - [Multicart]";
                    break;
                case 218:
                    mapperTextBox.Text = "218 - Single Chip Cartridge";
                    break;
                case 219:
                    mapperTextBox.Text = "219 - Garbage";
                    break;
                case 220:
                    mapperTextBox.Text = "220 - Summer Carnival 92";
                    break;
                case 221:
                    mapperTextBox.Text = "221 - Pirate Multicart";
                    break;
                case 222:
                case 223:
                case 224:
                    mapperTextBox.Text = mapperInt + " - [Pirate]";
                    break;
                case 225:
                    mapperTextBox.Text = "225 - Multicart";
                    exampleRomsTextBox.Text = "52 Games, 58-in-1, 64-in-1 ";
                    break;
                case 226:
                    mapperTextBox.Text = "226 - Multicart";
                    exampleRomsTextBox.Text = "76-in-1, Super 42-in-1 ";
                    break;
                case 227:
                    mapperTextBox.Text = "227 - Multicart";
                    exampleRomsTextBox.Text = "1200-in-1";
                    break;
                case 228:
                    mapperTextBox.Text = "228 - Action 52";
                    exampleRomsTextBox.Text = "Action 52, Cheetah Men II";
                    break;
                case 229:
                    mapperTextBox.Text = "229 - Pirate Multicart";
                    break;
                case 230:
                    mapperTextBox.Text = "230 - Multicart";
                    exampleRomsTextBox.Text = "22-in-1";
                    break;
                case 231:
                    mapperTextBox.Text = "231 - Multicart";
                    exampleRomsTextBox.Text = "20-in-1";
                    break;
                case 232:
                    mapperTextBox.Text = "232 - Camerica";
                    exampleRomsTextBox.Text = "Quattro Adventure, Quattro Sports, Quattro Arcade ";
                    break;
                case 233:
                    mapperTextBox.Text = "233 - Multicart";
                    exampleRomsTextBox.Text = "42-in-1";
                    break;
                case 234:
                    mapperTextBox.Text = "234 - Misc";
                    exampleRomsTextBox.Text = "Maxi 15 (PAL)";
                    break;
                case 235:
                    mapperTextBox.Text = "235 - 260 in 1";
                    break;
                case 236:
                    mapperTextBox.Text = "236 - Multicart";
                    break;
                case 238:
                    mapperTextBox.Text = "238 - Garbage";
                    break;
                case 240:
                    mapperTextBox.Text = "240 - Misc. (CN)";
                    exampleRomsTextBox.Text = "Jing Ke Xin Zhuan, Sheng Huo Lie Zhuan";
                    break;
                case 241:
                    mapperTextBox.Text = "241 - Misc. (CN)";
                    break;
                case 242:
                    mapperTextBox.Text = "242 - Misc. (CN)";
                    exampleRomsTextBox.Text = "Wai Xing Zhan Shi";
                    break;
                case 243:
                    mapperTextBox.Text = "243 - Misc";
                    exampleRomsTextBox.Text = "Honey, Poker III 5-in-1";
                    break;
                case 244:
                    mapperTextBox.Text = "244 - Decathlon";
                    break;
                case 245:
                    mapperTextBox.Text = "245 - Chinese WXN";
                    exampleRomsTextBox.Text = "Chu Han Zheng Ba - The War Between Chu & Han, Xing Ji Wu Shi - Super Fighter, Yin He Shi Dai, Yong Zhe Dou e Long - Dragon Quest VII (As), Dong Fang de Chuan Shuo - The Hyrule Fantasy";
                    break;
                case 246:
                    mapperTextBox.Text = "246 - Misc. (CN)";
                    exampleRomsTextBox.Text = "Fong Shen Bang - Zhu Lu Zhi Zhan";
                    break;
                case 248:
                    mapperTextBox.Text = "248 - Misc.";
                    break;
                case 249:
                    mapperTextBox.Text = "249 - Misc. (CN)";
                    break;
                case 250:
                    mapperTextBox.Text = "250 - Time Diver Avenger";
                    exampleRomsTextBox.Text = "Time Diver Avenger";
                    break;
                case 251:
                    mapperTextBox.Text = "251 - Pirate Multicart";
                    break;
                case 252:
                    mapperTextBox.Text = "252 - Chinese WXN";
                    break;
                case 253:
                    mapperTextBox.Text = "253 - Misc. (CN)";
                    break;
                case 255:
                    mapperTextBox.Text = "255 - Multicart";
                    break;
                default:
                    mapperTextBox.Text = mapperInt + " - Unknown";
                    break;
            }


            #endregion

            #region Byte 6 and 7 - four screen mode, trainer, SRAM, mirroring, playchoice 10, vs. unisystem, NES 2.0
            /*
             * Byte 6:
             * 7       0
             * ---------
             * NNNN FTBM
             * Bit 7: N: Lower 4 bits of the mapper number
             * Bit 6: N: Lower 4 bits of the mapper number
             * Bit 5: N: Lower 4 bits of the mapper number
             * Bit 4: N: Lower 4 bits of the mapper number
             * Bit 3: F: Four screen mode. 0 = no, 1 = yes. (When set, the M bit has no effect)
             * Bit 2: T: Trainer.  0 = no trainer present, 1 = 512 byte trainer at 7000-71FFh
             * Bit 1: B: SRAM at 6000-7FFFh battery backed.  0= no, 1 = yes
             * Bit 0: M: Mirroring.  0 = horizontal, 1 = vertical.
             * 
             * Byte 7:
             * 7       0
             * ---------
             * NNNN SSPV
             * Bit 7: N: Upper 4 bits of the mapper number
             * Bit 6: N: Upper 4 bits of the mapper number
             * Bit 5: N: Upper 4 bits of the mapper number
             * Bit 4: N: Upper 4 bits of the mapper number
             * Bit 3: S: these bits are not used in iNES. <- NOTE: When equal to binary 10, 
             * Bit 2: S: these bits are not used in iNES. <- use NES 2.0 rules; otherwise, use other rules.
             * Bit 1: P: Playchoice 10.  When set, this is a PC-10 game
             * Bit 0: V: Vs. Unisystem.  When set, this is a Vs. game
             */

            // Lower part of byte 6
            // Upper part of byte 7

            string lowerNibbleByte6BinaryString = hexStringToBinary(lowerNibbleByteSixString);
            string lowerNibbleByte7BinaryString = hexStringToBinary(lowerNibbleByteSevenString);

            char[] lowerNibbleByte6BinaryCharArray = lowerNibbleByte6BinaryString.ToCharArray();
            char[] lowerNibbleByte7BinaryCharArray = lowerNibbleByte7BinaryString.ToCharArray();

            // 4 screen mode
            if (lowerNibbleByte6BinaryCharArray[0].ToString() == "0")
            {
                textBoxFourScreenMode.Text = "NO";
            }
            else // 1
            {
                textBoxFourScreenMode.Text = "YES";
            }

            // Trainer
            if (lowerNibbleByte6BinaryCharArray[1].ToString() == "0")
            {
                trainerTextBox.Text = "NO";
            }
            else // 1
            {
                trainerTextBox.Text = "YES";
            }

            // SRAM
            if (lowerNibbleByte6BinaryCharArray[2].ToString() == "0")
            {
                batteryTextBox.Text = "NO";
            }
            else // 1
            {
                batteryTextBox.Text = "YES";
            }

            // Mirroring
            if (lowerNibbleByte6BinaryCharArray[3].ToString() == "0")
            {
                mirroringTextBox.Text = "Horizontal Mirroring";
            }
            else // 1
            {
                mirroringTextBox.Text = "Vertical Mirroring";
            }

            // Playchoice 10
            if (lowerNibbleByte7BinaryCharArray[2].ToString() == "0")
            {
                textBoxPlayChoice10.Text = "NO";
            }
            else // 1
            {
                textBoxPlayChoice10.Text = "YES";
            }

            // Vs. Unisystem
            if (lowerNibbleByte7BinaryCharArray[3].ToString() == "0")
            {
                textBoxVSUnisystem.Text = "NO";
            }
            else // 1
            {
                textBoxVSUnisystem.Text = "YES";
            }

            // Use NES 2.0
            if (lowerNibbleByte7BinaryCharArray[0].ToString() == "1" && lowerNibbleByte7BinaryCharArray[1].ToString() == "0")
            {
                nes20Supported = true;
            }
            #endregion

            // debug
            //nes20Supported = true;

            #region NES 2.0
            if (nes20Supported)
            {

                #region byte 8 - Mapper variant
                /*
                 * 7       0
                 * ---------
                 * SSSS MMMM
                 * 
                 * S: Submapper number.  Mappers not using submappers set this to zero.
                 * M: Bits 11-8 of mapper number.
                 * 
                 * Submappers are used to disambiguate iNES 1 mappers that require multiple incompatible implementations. Most mappers will not use submappers; they set S to 0.
                 * 
                 * It's not recommended yet as of 2015 to assign mapper numbers greater than 255. There were still a couple dozen existing numbers left in the current iNES mapper space as of 2013.
                 * Mapper numbers up to 511 should hold us until they stop making Ice Age films; mapper numbers up to 4095 should hold us until the next literal ice age.
                 * 
                 * In 2013, there was a proposal on the BBS to break up the expanded mapper space into "planes", much like those of Unicode when it expanded past UCS-2.
                 * Each M value would thus correspond to one plane:
                 * 
                 *     Plane 0 (0-255): Basic Multilingual Plane, the current mess
                 *     Plane 1 (256-511): Mostly for new homebrew mappers.
                 *     Plane 2 (512-767): For new dumps of East Asian games.
                 *     Plane 15: Private use area (not for publicly distributed dumps)
                 */

                string lowerNibbleByte8BinaryString = hexStringToBinary(lowerNibbleByteEightString);
                string upperNibbleByte8BinaryString = hexStringToBinary(upperNibbleByteEightString);

                int subMapperNumber = Convert.ToInt32(upperNibbleByteEightString);
                int plane = Convert.ToInt32(lowerNibbleByteEightString);

                submapperTextBox.Text = subMapperNumber.ToString();

                if (subMapperNumber == 0 && mapperInt == 1)
                {
                    submapperInfoTextBox.Text = "Default Behavior";
                }
                else if (subMapperNumber == 1 && mapperInt == 1)
                {
                    submapperInfoTextBox.Text = "Deprecated: SUROM";
                }
                else if (subMapperNumber == 2 && mapperInt == 1)
                {
                    submapperInfoTextBox.Text = "Deprecated: SOROM";
                }
                else if (subMapperNumber == 3 && mapperInt == 1)
                {
                    submapperInfoTextBox.Text = "Deprecated: Already implemented as iNES Mapper 155.";
                }
                else if (subMapperNumber == 4 && mapperInt == 1)
                {
                    submapperInfoTextBox.Text = "Deprecated: SXROM";
                }
                else if (subMapperNumber == 5 && mapperInt == 1)
                {
                    submapperInfoTextBox.Text = "Fixed PRG: SEROM, SHROM, SH1ROM use a fixed 32k PRG ROM with no banking support.";
                }
                else if (subMapperNumber == 0 && mapperInt == 2)
                {
                    submapperInfoTextBox.Text = "UNROM/UN1ROM/UOROM - Default Behavior";
                }
                else if (subMapperNumber == 1 && mapperInt == 2)
                {
                    submapperInfoTextBox.Text = "UNROM/UN1ROM/UOROM - Bus conflicts do not occur";
                }
                else if (subMapperNumber == 2 && mapperInt == 2)
                {
                    submapperInfoTextBox.Text = "UNROM/UN1ROM/UOROM - Bus conflicts occur, producing the bitwise AND of the written value and the value in ROM";
                }
                else if (subMapperNumber == 0 && mapperInt == 3)
                {
                    submapperInfoTextBox.Text = "CNROM - Default Behavior";
                }
                else if (subMapperNumber == 1 && mapperInt == 3)
                {
                    submapperInfoTextBox.Text = "CNROM - Bus conflicts do not occur";
                }
                else if (subMapperNumber == 2 && mapperInt == 3)
                {
                    submapperInfoTextBox.Text = "CNROM - Bus conflicts occur, producing the bitwise AND of the written value and the value in ROM";
                }
                else if (subMapperNumber == 0 && mapperInt == 4)
                {
                    submapperInfoTextBox.Text = "MMC3C - Default Behavior";
                }
                else if (subMapperNumber == 1 && mapperInt == 4)
                {
                    submapperInfoTextBox.Text = "MMC6 - Alternative PRG-RAM enable and write protection scheme designed for its internal 1k PRG RAM. ";
                }
                else if (subMapperNumber == 2 && mapperInt == 4)
                {
                    submapperInfoTextBox.Text = "Deprecated: MMC3C - with hard wired mirroring. No games are known to require this. ";
                }
                else if (subMapperNumber == 3 && mapperInt == 4)
                {
                    submapperInfoTextBox.Text = "MC-ACC - Found in 13 second-source PCBs manufactured by Acclaim. ";
                }
                else if (subMapperNumber == 0 && mapperInt == 7)
                {
                    submapperInfoTextBox.Text = "ANROM/AN1ROM/AOROM/AMROM - Default Behavior";
                }
                else if (subMapperNumber == 1 && mapperInt == 7)
                {
                    submapperInfoTextBox.Text = "ANROM/AN1ROM/AOROM/AMROM - Bus conflicts do not occur";
                }
                else if (subMapperNumber == 2 && mapperInt == 7)
                {
                    submapperInfoTextBox.Text = "ANROM/AN1ROM/AOROM/AMROM - Bus conflicts occur, producing the bitwise AND of the written value and the value in ROM";
                }
                else if (subMapperNumber == 0 && mapperInt == 32)
                {
                    submapperInfoTextBox.Text = "Irem G101 - Normal (H/V mapper-controlled mirroring) ";
                }
                else if (subMapperNumber == 1 && mapperInt == 32)
                {
                    submapperInfoTextBox.Text = "Major League - CIRAM A10 is tied high (fixed one-screen mirroring) and PRG banking style is fixed as 8+8+16F";
                }
                else if (subMapperNumber == 0 && mapperInt == 34)
                {
                    submapperInfoTextBox.Text = "BNROM/NINA-001 - Default Behavior";
                }
                else if (subMapperNumber == 1 && mapperInt == 34)
                {
                    submapperInfoTextBox.Text = "NINA-001";
                }
                else if (subMapperNumber == 2 && mapperInt == 34)
                {
                    submapperInfoTextBox.Text = "BNROM - Some unlicensed boards by Union Bond were a variation of BNROM that included PRG RAM. These may also use this submapper if PRG RAM is specified in the NES 2.0 header.";
                }
                else if (subMapperNumber == 0 && mapperInt == 68)
                {
                    submapperInfoTextBox.Text = "Sunsoft 4 - Normal (max 256KiB PRG)";
                }
                else if (subMapperNumber == 1 && mapperInt == 68)
                {
                    submapperInfoTextBox.Text = "Sunsoft 4 - Sunsoft Dual Cartridge System a.k.a. NTB-ROM (max 128KiB PRG, licensing IC present, external option ROM of up to 128KiB should be selectable by a second menu)";
                }
                else if (subMapperNumber == 0 && mapperInt == 71)
                {
                    submapperInfoTextBox.Text = "Codemasters - Hardwired horizontal or vertical mirroring. ";
                }
                else if (subMapperNumber == 1 && mapperInt == 71)
                {
                    submapperInfoTextBox.Text = "Codemasters - Fire Hawk - Mapper controlled single-screen mirroring.";
                }
                else if (subMapperNumber == 0 && mapperInt == 78)
                {
                    submapperInfoTextBox.Text = "Unspecified. ";
                }
                else if (subMapperNumber == 1 && mapperInt == 78)
                {
                    submapperInfoTextBox.Text = "Cosmo Carrier - Single-screen mirroring (nibble-swapped mapper 152). ";
                }
                else if (subMapperNumber == 2 && mapperInt == 78)
                {
                    submapperInfoTextBox.Text = "Deprecated - This described a variation with fixed vertical mirroring, and WRAM. There is no known use case. ";
                }
                else if (subMapperNumber == 3 && mapperInt == 78)
                {
                    submapperInfoTextBox.Text = "Holy Diver - Mapper-controlled H/V mirroring.";
                }
                else if (subMapperNumber == 0 && mapperInt == 210)
                {
                    submapperInfoTextBox.Text = "No advisory statement is made (use runtime heuristics suggested at mapper 210).";
                }
                else if (subMapperNumber == 1 && mapperInt == 210)
                {
                    submapperInfoTextBox.Text = "N175 - Namco 175. Hardwired mirroring, no IRQ. ";
                }
                else if (subMapperNumber == 2 && mapperInt == 210)
                {
                    submapperInfoTextBox.Text = "N340 - Namco 340. 1/H/V mirroring, no IRQ, no internal or external RAM. ";
                }
                else if (subMapperNumber == 0 && mapperInt == 232)
                {
                    submapperInfoTextBox.Text = "Normal";
                }
                else if (subMapperNumber == 1 && mapperInt == 232)
                {
                    submapperInfoTextBox.Text = "Aladdin Deck Enhancer - Aladdin Deck Enhancer variation. Swap the bits of the outer bank number.";
                }
                else
                {
                    submapperInfoTextBox.Text = "Nothing defined for this submapper.";
                }

                if (lowerNibbleByte8BinaryString == "0000")
                {
                    planeTextBox.Text = "0 (0-255): Basic Multilingual.";
                }
                else if (lowerNibbleByte8BinaryString == "0001")
                {
                    planeTextBox.Text = "1 (256-511): Mostly for new homebrew mappers.";
                }
                else if (lowerNibbleByte8BinaryString == "0010")
                {
                    planeTextBox.Text = "2 (512-767): For new dumps of East Asian games.";
                }
                else if (lowerNibbleByte8BinaryString == "1111")
                {
                    planeTextBox.Text = "15: Private use area (not for publicly distributed dumps).";
                }
                else
                {
                    // Not defined yet.
                    planeTextBox.Text = plane + ": Undefined.";
                }
                #endregion

                #region byte 9 - Upper bits of ROM size (4 more CHR ROM size bits, 4 more PRG ROM size bits)
                /*
                 * 7       0
                 * ---------
                 * CCCC PPPP
                 *
                 * C: 4 more CHR ROM size bits
                 * P: 4 more PRG ROM size bits
                 * 
                 * These combine with the existing 8 bits of each to form 12 bits total for the number of PRG and CHR banks... this is enough for 64Mbytes-16K of PRG data and 32Mbytes-8K of CHR data.
                 * 
                 * Only a few mappers, mostly multicart mappers, support non-power-of-two sizes for PRG and CHR. The behavior of a ROM with a Nintendo MMC and a non-power-of-two ROM size is undefined. 
                 */

                // 
                // binary to hex to decimal times 16384
                // I think it works like this:
                // get the lower nibble of byte 9: xxxx
                // get byte 4: xxxxxxxx
                // put them together as byte9byte4 xxxx xxxx xxxx
                // convert that binary number to decimal and multiply it by 16384

                // PRG 16384 * byte 4 + lower nibble of byte 9
                // CHR 8192 * byte 5 + uppwer nibble of byte 9


                //byte 4
                string lowerNibbleByte4BinaryString = hexStringToBinary(lowerNibbleByteFourString);
                string upperNibbleByte4BinaryString = hexStringToBinary(upperNibbleByteFourString);

                //byte 5
                string lowerNibbleByte5BinaryString = hexStringToBinary(lowerNibbleByteFiveString);
                string upperNibbleByte5BinaryString = hexStringToBinary(upperNibbleByteFiveString);

                // byte 9
                string lowerNibbleByte9BinaryString = hexStringToBinary(lowerNibbleByteNineString);
                string upperNibbleByte9BinaryString = hexStringToBinary(upperNibbleByteNineString);

                string extendedPRGSizeBinaryString = lowerNibbleByte9BinaryString + upperNibbleByte4BinaryString + lowerNibbleByte4BinaryString;
                string extendedCHRSizeBinaryString = upperNibbleByte9BinaryString + upperNibbleByte5BinaryString + lowerNibbleByte5BinaryString;

                int extendedPRGSizeInt = Convert.ToInt32(extendedPRGSizeBinaryString, 2);
                int extendedCHRSizeInt = Convert.ToInt32(extendedCHRSizeBinaryString, 2);

                int extendedPRGSize = extendedPRGSizeInt * 16384;
                int extendedCHRSize = extendedCHRSizeInt * 8192;

                prgExtendedTextBox.Text = extendedPRGSize.ToString();
                chrExtendedTextBox.Text = extendedCHRSize.ToString();

                #endregion

                #region byte 10 - RAM size, PRG RAM and Battery backed PRG RAM
                /*
                 * 7       0
                 * ---------
                 * pppp PPPP
                 *
                 * p: Quantity of PRG RAM which is battery backed (or serial EEPROM, see below)
                 * P: Quantity of PRG RAM which is NOT battery backed
                 */

                string lowerNibbleByte10BinaryString = hexStringToBinary(lowerNibbleByteTenString);
                string upperNibbleByte10BinaryString = hexStringToBinary(upperNibbleByteTenString);

                // PRG RAM
                if (lowerNibbleByte10BinaryString == "0000")
                {
                    prgRAMTextBox.Text = "0";
                }
                else if (lowerNibbleByte10BinaryString == "0001")
                {
                    prgRAMTextBox.Text = "128";
                }
                else if (lowerNibbleByte10BinaryString == "0010")
                {
                    prgRAMTextBox.Text = "256";
                }
                else if (lowerNibbleByte10BinaryString == "0011")
                {
                    prgRAMTextBox.Text = "512";
                }
                else if (lowerNibbleByte10BinaryString == "0100")
                {
                    prgRAMTextBox.Text = "1024";
                }
                else if (lowerNibbleByte10BinaryString == "0101")
                {
                    prgRAMTextBox.Text = "2048";
                }
                else if (lowerNibbleByte10BinaryString == "0110")
                {
                    prgRAMTextBox.Text = "4096";
                }
                else if (lowerNibbleByte10BinaryString == "0111")
                {
                    prgRAMTextBox.Text = "8192";
                }
                else if (lowerNibbleByte10BinaryString == "1000")
                {
                    prgRAMTextBox.Text = "16384";
                }
                else if (lowerNibbleByte10BinaryString == "1001")
                {
                    prgRAMTextBox.Text = "32768";
                }
                else if (lowerNibbleByte10BinaryString == "1010")
                {
                    prgRAMTextBox.Text = "65536";
                }
                else if (lowerNibbleByte10BinaryString == "1011")
                {
                    prgRAMTextBox.Text = "131072";
                }
                else if (lowerNibbleByte10BinaryString == "1100")
                {
                    prgRAMTextBox.Text = "262144";
                }
                else if (lowerNibbleByte10BinaryString == "1101")
                {
                    prgRAMTextBox.Text = "524288";
                }
                else if (lowerNibbleByte10BinaryString == "1110")
                {
                    prgRAMTextBox.Text = "1048576";
                }
                else if (lowerNibbleByte10BinaryString == "1111")
                {
                    prgRAMTextBox.Text = "Reserved";
                }

                // Battery backed PRG RAM
                if (upperNibbleByte10BinaryString == "0000")
                {
                    batteryBackedPRGRAMTextBox.Text = "0";
                }
                else if (upperNibbleByte10BinaryString == "0001")
                {
                    batteryBackedPRGRAMTextBox.Text = "128";
                }
                else if (upperNibbleByte10BinaryString == "0010")
                {
                    batteryBackedPRGRAMTextBox.Text = "256";
                }
                else if (upperNibbleByte10BinaryString == "0011")
                {
                    batteryBackedPRGRAMTextBox.Text = "512";
                }
                else if (upperNibbleByte10BinaryString == "0100")
                {
                    batteryBackedPRGRAMTextBox.Text = "1024";
                }
                else if (upperNibbleByte10BinaryString == "0101")
                {
                    batteryBackedPRGRAMTextBox.Text = "2048";
                }
                else if (upperNibbleByte10BinaryString == "0110")
                {
                    batteryBackedPRGRAMTextBox.Text = "4096";
                }
                else if (upperNibbleByte10BinaryString == "0111")
                {
                    batteryBackedPRGRAMTextBox.Text = "8192";
                }
                else if (upperNibbleByte10BinaryString == "1000")
                {
                    batteryBackedPRGRAMTextBox.Text = "16384";
                }
                else if (upperNibbleByte10BinaryString == "1001")
                {
                    batteryBackedPRGRAMTextBox.Text = "32768";
                }
                else if (upperNibbleByte10BinaryString == "1010")
                {
                    batteryBackedPRGRAMTextBox.Text = "65536";
                }
                else if (upperNibbleByte10BinaryString == "1011")
                {
                    batteryBackedPRGRAMTextBox.Text = "131072";
                }
                else if (upperNibbleByte10BinaryString == "1100")
                {
                    batteryBackedPRGRAMTextBox.Text = "262144";
                }
                else if (upperNibbleByte10BinaryString == "1101")
                {
                    batteryBackedPRGRAMTextBox.Text = "524288";
                }
                else if (upperNibbleByte10BinaryString == "1110")
                {
                    batteryBackedPRGRAMTextBox.Text = "1048576";
                }
                else if (upperNibbleByte10BinaryString == "1111")
                {
                    batteryBackedPRGRAMTextBox.Text = "Reserved";
                }

                #endregion

                #region byte 11 - Video RAM size, CHR RAM and Battery backed CHR RAM
                /*
                 * 7       0
                 * ---------
                 * cccc CCCC
                 *
                 * c: Quantity of CHR RAM which is battery backed (yes it exists! see below)
                 * C: Quantity of CHR RAM which is NOT battery backed
                 *
                 * The majority of games with no CHR ROM will have $07 (8192 bytes, not battery backed) in this byte. Use of $00 with no CHR ROM implies that the game is wired to map nametable memory in CHR space. 
                 * The value $00 MUST NOT be used if a mapper isn't defined to allow this. Battery-backed CHR RAM exists. The RacerMate Challenge II cartridge has 64K of CHR RAM total: 32K is battery backed, and 
                 * 32K of it is not. They store all the stats and such in it. KH traced out the circuit and couldn't believe it. It probably simplified the routing or power off protection. For backward compatibility, 
                 * the battery bit in the original iNES header (byte 6, bit 1) MUST be true if the upper nibble of byte 10 or 11 is nonzero or false otherwise.
                */

                string lowerNibbleByte11BinaryString = hexStringToBinary(lowerNibbleByteElevenString);
                string upperNibbleByte11BinaryString = hexStringToBinary(upperNibbleByteElevenString);

                // CHR RAM
                if (lowerNibbleByte11BinaryString == "0000")
                {
                    chrRAMTextBox.Text = "0";
                }
                else if (lowerNibbleByte11BinaryString == "0001")
                {
                    chrRAMTextBox.Text = "128";
                }
                else if (lowerNibbleByte11BinaryString == "0010")
                {
                    chrRAMTextBox.Text = "256";
                }
                else if (lowerNibbleByte11BinaryString == "0011")
                {
                    chrRAMTextBox.Text = "512";
                }
                else if (lowerNibbleByte11BinaryString == "0100")
                {
                    chrRAMTextBox.Text = "1024";
                }
                else if (lowerNibbleByte11BinaryString == "0101")
                {
                    chrRAMTextBox.Text = "2048";
                }
                else if (lowerNibbleByte11BinaryString == "0110")
                {
                    chrRAMTextBox.Text = "4096";
                }
                else if (lowerNibbleByte11BinaryString == "0111")
                {
                    chrRAMTextBox.Text = "8192";
                }
                else if (lowerNibbleByte11BinaryString == "1000")
                {
                    chrRAMTextBox.Text = "16384";
                }
                else if (lowerNibbleByte11BinaryString == "1001")
                {
                    chrRAMTextBox.Text = "32768";
                }
                else if (lowerNibbleByte11BinaryString == "1010")
                {
                    chrRAMTextBox.Text = "65536";
                }
                else if (lowerNibbleByte11BinaryString == "1011")
                {
                    chrRAMTextBox.Text = "131072";
                }
                else if (lowerNibbleByte11BinaryString == "1100")
                {
                    chrRAMTextBox.Text = "262144";
                }
                else if (lowerNibbleByte11BinaryString == "1101")
                {
                    chrRAMTextBox.Text = "524288";
                }
                else if (lowerNibbleByte11BinaryString == "1110")
                {
                    chrRAMTextBox.Text = "1048576";
                }
                else if (lowerNibbleByte11BinaryString == "1111")
                {
                    chrRAMTextBox.Text = "Reserved";
                }

                // Battery backed PRG RAM
                if (upperNibbleByte11BinaryString == "0000")
                {
                    batteryBackedCHRRAMTextBox.Text = "0";
                }
                else if (upperNibbleByte11BinaryString == "0001")
                {
                    batteryBackedCHRRAMTextBox.Text = "128";
                }
                else if (upperNibbleByte11BinaryString == "0010")
                {
                    batteryBackedCHRRAMTextBox.Text = "256";
                }
                else if (upperNibbleByte11BinaryString == "0011")
                {
                    batteryBackedCHRRAMTextBox.Text = "512";
                }
                else if (upperNibbleByte11BinaryString == "0100")
                {
                    batteryBackedCHRRAMTextBox.Text = "1024";
                }
                else if (upperNibbleByte11BinaryString == "0101")
                {
                    batteryBackedCHRRAMTextBox.Text = "2048";
                }
                else if (upperNibbleByte11BinaryString == "0110")
                {
                    batteryBackedCHRRAMTextBox.Text = "4096";
                }
                else if (upperNibbleByte11BinaryString == "0111")
                {
                    batteryBackedCHRRAMTextBox.Text = "8192";
                }
                else if (upperNibbleByte11BinaryString == "1000")
                {
                    batteryBackedCHRRAMTextBox.Text = "16384";
                }
                else if (upperNibbleByte11BinaryString == "1001")
                {
                    batteryBackedCHRRAMTextBox.Text = "32768";
                }
                else if (upperNibbleByte11BinaryString == "1010")
                {
                    batteryBackedCHRRAMTextBox.Text = "65536";
                }
                else if (upperNibbleByte11BinaryString == "1011")
                {
                    batteryBackedCHRRAMTextBox.Text = "131072";
                }
                else if (upperNibbleByte11BinaryString == "1100")
                {
                    batteryBackedCHRRAMTextBox.Text = "262144";
                }
                else if (upperNibbleByte11BinaryString == "1101")
                {
                    batteryBackedCHRRAMTextBox.Text = "524288";
                }
                else if (upperNibbleByte11BinaryString == "1110")
                {
                    batteryBackedCHRRAMTextBox.Text = "1048576";
                }
                else if (upperNibbleByte11BinaryString == "1111")
                {
                    batteryBackedCHRRAMTextBox.Text = "Reserved";
                }

                #endregion

                #region byte 12 - TV System, NTSC/PAL or Dendy PAL/Both

                /*
                 * Different TV systems have different clock rates, and a game's raster effects and difficulty tuning might expect one or the other.
                 * 7       0
                 * ---------
                 * xxxx xxBP
                 *
                 * P: 0 indicates NTSC mode; 1 is for PAL mode.
                 *
                 * NTSC mode - 262 lines, NMI on line 241, 3 dots per CPU clock
                 * PAL mode - 312 lines, NMI on line 241, 3.2 dots per CPU clock
                 * Dendy PAL mode - 312 lines, NMI on line 291, 3 dots per CPU clock, designed for maximum compatibility with NTSC ROMs, but NES 2.0 does not yet indicate that a game is designed for this mode
                 *
                 * B: When set, indicates this ROM works on both PAL and NTSC machines. Some of the Codemasters games actually will adjust the game depending on if it detects you running on a PAL or NTSC machine
                 * - it adjusts the timing of the game and transposes the music. Not many games would have this B flag set; those that do would be labeled (UE) or the like in GoodNES. 
                 */

                string lowerNibbleByte12BinaryString = hexStringToBinary(lowerNibbleByteTwelveString);
                string upperNibbleByte12BinaryString = hexStringToBinary(upperNibbleByteTwelveString);

                char[] lowerNibbleByte12BinaryCharArray = lowerNibbleByte12BinaryString.ToCharArray();
                char[] upperNibbleByte12BinaryCharArray = upperNibbleByte12BinaryString.ToCharArray();

                // TV System
                if (lowerNibbleByte12BinaryCharArray[2].ToString() == "0")
                {
                    tvSystemTextBox.Text = "NTSC";
                }
                else // 1
                {
                    tvSystemTextBox.Text = "PAL";
                }

                if (lowerNibbleByte12BinaryCharArray[3].ToString() == "1")
                {
                    tvSystemTextBox.Text = "NTSC/PAL";
                }

                #endregion

                #region byte 13 - Vs. Hardware
                /*
                 * 7       0
                 * ---------
                 * MMMM PPPP
                 *
                 * This byte is reserved for the Vs. system only.  If this is not
                 * a Vs. system ROM, the value of this byte must be $00, which
                 * signifies RP2C03B (used in PlayChoice, Famicom Titler, and a
                 * few TVs with built-in Famicom) and no Vs.-specific submapper.
                 *
                 * P: PPU.  There are 13 Vs. PPUs that can be found on the games:
                 *
                 *  0 - RP2C03B     (bog standard RGB palette)
                 *  1 - RP2C03G     (similar pallete to above, might have 1 changed colour)
                 *  2 - RP2C04-0001 (scrambled palette + new colours)
                 *  3 - RP2C04-0002 (same as above, different scrambling)
                 *  4 - RP2C04-0003 (similar to above)
                 *  5 - RP2C04-0004 (similar to above)
                 *  6 - RC2C03B     (bog standard palette, seems identical to RP2C03B)
                 *  7 - RC2C03C     (similar to above, but with 1 changed colour or so)
                 *  8 - RC2C05-01   (all five of these have the normal palette...
                 *  9 - RC2C05-02   (...but with different bits returned on 2002)
                 *  10 - RC2C05-03
                 *  11 - RC2C05-04
                 *  12 - RC2C05-05
                 *  13 - not defined (do not use these)
                 *  14 - not defined
                 *  15 - not defined

                 * KH has a good cross-section of Vs. games and has dumped bit-for-bit
                 * palettes from all thirteen of these PPUs.  The last 5 PPUs (RC2C05)
                 * have the standard NES palette in them, however they return a specific
                 * word in the lower 5 bits of PPUSTATUS ($2002), and the PPUCTRL ($2000)
                 * and PPUMASK ($2001) ports are flipped around (PPUMASK at $2000 and 
                 * PPUCTRL at $2001).
                 *
                 * Nocash and MESS report:
                 *  RC2C05-01 (with ID ([2002h] AND ??h)=1Bh)
                 *  RC2C05-02 (with ID ([2002h] AND 3Fh)=3Dh)
                 *  RC2C05-03 (with ID ([2002h] AND 1Fh)=1Ch)
                 *  RC2C05-04 (with ID ([2002h] AND 1Fh)=1Bh)
                 *  and cannot find the 2C05-05


                 * M: Vs. mode:

                 *  0 - Normal- no special mode(s)
                 *  1 - RBI Baseball  (protection hardware at port 5E0xh)
                 *  2 - TKO Boxing    (other protection hardware at port 5E0xh)
                 *  3 - Super Xevious (protection hardware at port 5xxxh)
                 *  4 - ...
                 */

                if (lowerNibbleByte7BinaryCharArray[3].ToString() == "1")
                {

                    string lowerNibbleByte13BinaryString = hexStringToBinary(lowerNibbleByteThirteenString);
                    string upperNibbleByte13BinaryString = hexStringToBinary(upperNibbleByteThirteenString);

                    // PPU
                    if (lowerNibbleByte13BinaryString == "0000")
                    {
                        vsPPUTextBox.Text = "RP2C03B - (bog standard RGB palette)";
                    }
                    else if (lowerNibbleByte13BinaryString == "0001")
                    {
                        vsPPUTextBox.Text = "RP2C03G - (similar to bog standard RGB palette, might have 1 changed color)";
                    }
                    else if (lowerNibbleByte13BinaryString == "0010")
                    {
                        vsPPUTextBox.Text = "RP2C04-0001 - (scrambled palette + new colors)";
                    }
                    else if (lowerNibbleByte13BinaryString == "0011")
                    {
                        vsPPUTextBox.Text = "RP2C04-0002 - (scrambled palette + new colors)";
                    }
                    else if (lowerNibbleByte13BinaryString == "0100")
                    {
                        vsPPUTextBox.Text = "RP2C04-0003 - (scrambled palette + new colors)";
                    }
                    else if (lowerNibbleByte13BinaryString == "0101")
                    {
                        vsPPUTextBox.Text = "RP2C04-0004 - (scrambled palette + new colors)";
                    }
                    else if (lowerNibbleByte13BinaryString == "0110")
                    {
                        vsPPUTextBox.Text = "RC2C03B - (bog standard palette, seems identical to RP2C03B)";
                    }
                    else if (lowerNibbleByte13BinaryString == "0111")
                    {
                        vsPPUTextBox.Text = "RC2C03C - (similar to bog standard RGB palette, might have 1 changed color)";
                    }
                    else if (lowerNibbleByte13BinaryString == "1000")
                    {
                        vsPPUTextBox.Text = "RC2C05-01 - (normal palette, (with ID ([2002h] AND ??h)=1Bh)";
                    }
                    else if (lowerNibbleByte13BinaryString == "1001")
                    {
                        vsPPUTextBox.Text = "RC2C05-02 - (normal palette, (with ID ([2002h] AND 3Fh)=3Dh)";
                    }
                    else if (lowerNibbleByte13BinaryString == "1010")
                    {
                        vsPPUTextBox.Text = "RC2C05-03 - (normal palette, (with ID ([2002h] AND 1Fh)=1Ch)";
                    }
                    else if (lowerNibbleByte13BinaryString == "1011")
                    {
                        vsPPUTextBox.Text = "RC2C05-04 - (normal palette, (with ID ([2002h] AND 1Fh)=1Bh)";
                    }
                    else if (lowerNibbleByte13BinaryString == "1100")
                    {
                        vsPPUTextBox.Text = "RC2C05-05 - (normal palette, (with ID ([2002h] AND ??h)=??h)";
                    }
                    else
                    {
                        vsPPUTextBox.Text = "Not Defined";
                    }

                    // Vs. Mode
                    if (upperNibbleByte13BinaryString == "0000")
                    {
                        vsModeTextBox.Text = "Normal- no special mode(s)";
                    }
                    else if (upperNibbleByte13BinaryString == "0001")
                    {
                        vsModeTextBox.Text = "RBI Baseball - (protection hardware at port 5E0xh)";
                    }
                    else if (upperNibbleByte13BinaryString == "0010")
                    {
                        vsModeTextBox.Text = "TKO Boxing - (protection hardware at port 5E0xh)";
                    }
                    else if (upperNibbleByte13BinaryString == "0011")
                    {
                        vsModeTextBox.Text = "Super Xevious - (protection hardware at port 5xxxh)";
                    }
                    else
                    {
                        vsModeTextBox.Text = "Not Defined";
                    }
                }
                else
                {
                    vsPPUTextBox.Text = "Not Defined";
                    vsModeTextBox.Text = "Not Defined";
                }

                #endregion

                #region byte 14 & byte 15 - Reserved
                /*
                 * Reserved, these two bytes must be zero. 
                 */
                #endregion
            }
            else
            {
                submapperInfoTextBox.Text = "NES 2.0 Header was not found.";
            }
            #endregion
        }

        public string hexStringToBinary(string hex)
        {
            StringBuilder result = new StringBuilder();
            foreach (char c in hex)
            {
                // This will crash for non-hex characters.
                result.Append(hexCharacterToBinary[char.ToLower(c)]);
            }
            return result.ToString();
        }

        private void cleanRomButton_Click(object sender, EventArgs e)
        {
            try
            {
                string s = "\0\0\0\0\0\0\0\0\0";
                byte[] g = Encoding.Default.GetBytes(s);
                long offset = Convert.ToInt64("7");
                editFile(path, offset, g);
                openFlagString = "1";
                MessageBox.Show("Successfully cleaned ROM.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to clean ROM: " + ex, "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            openToolStripMenuItem_Click(sender, e);

        }

        private void editFile(string path, long offset, byte[] g)
        {
            FileStream f = new FileStream(path, FileMode.Open);
            f.Seek(offset, SeekOrigin.Begin);
            f.Write(g, 0, g.Length);
            f.Close();
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            openFlagString = "0";
        }

        #region checkboxes
        private void remove16byteHeaderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (remove16byteHeaderCheckBox.Checked)
            {
                outputCHRPRGCheckBox.Enabled = true;
                prepRomButton.Enabled = true;
            }
            else
            {
                outputCHRPRGCheckBox.Enabled = false;
                outputCHRPRGCheckBox.Checked = false;
                prepRomButton.Enabled = false;
            }
        }

        private void chrprgCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (outputCHRPRGCheckBox.Checked)
            {
                autoSplitChrPrgRadioButton.Enabled = true;
                manualSplitPrgChrRadioButton.Enabled = true;
                eightKBSplitPrgChrRadioButton.Enabled = true;

                autoSplitChrPrgRadioButton.Checked = true;
            }
            else
            {
                autoSplitChrPrgRadioButton.Enabled = false;
                manualSplitPrgChrRadioButton.Enabled = false;
                eightKBSplitPrgChrRadioButton.Enabled = false;
                prgSizeSplitTextBox.Enabled = false;
                chrSizeSplitTextBox.Enabled = false;
                
                autoSplitChrPrgRadioButton.Checked = false;
                manualSplitPrgChrRadioButton.Checked = false;
                eightKBSplitPrgChrRadioButton.Checked = false;
            }
        }
        #endregion

        #region radio buttons
        private void autoSplitChrPrgRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (autoSplitChrPrgRadioButton.Checked)
            {
                prgSizeSplitTextBox.Enabled = false;
                chrSizeSplitTextBox.Enabled = false;

                // Lazy way to recalculate the correct sizes.
                analyzeRom();
            }
        }

        private void manualSplitPrgChrRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (manualSplitPrgChrRadioButton.Checked)
            {
                prgSizeSplitTextBox.Enabled = true;
                chrSizeSplitTextBox.Enabled = true;
            }
        }

        private void eightKBSplitPrgChrRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (eightKBSplitPrgChrRadioButton.Checked)
            {
                prgSizeSplitTextBox.Enabled = false;
                chrSizeSplitTextBox.Enabled = false;

                // Lazy way to recalculate the correct sizes.
                analyzeRom();
            }
        }
        #endregion

        #region toolstrips
        private void analyzeRomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            analyzeRomButton_Click(sender, e);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox1 aboutBox = new AboutBox1();

            aboutBox.ShowDialog();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (hexTextBox.SelectionLength == 0)
            {
                hexTextBox.SelectAll();
            }

            hexTextBox.Copy();
        }

        private void loadROMButton_Click(object sender, EventArgs e)
        {
            openToolStripMenuItem_Click(sender, e);
        }
        #endregion
        #endregion

    }
}
