using CUE.NET;
using CUE.NET.Devices.Generic;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Devices.Keyboard;
using CUE.NET.Devices.Keyboard.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Net.Mime.MediaTypeNames;

namespace GuitarHeroCUE
{
    class Program
    {
        static CorsairLedId[,] cleds = new CorsairLedId[10, 4];

        static float build_id = 0.1f;

        public static List<Note> notes = new List<Note>();

        public static bool n1 = false, nq = false, na = false, nz = false;

        static float health = 12.0f;

        public static bool ended = false;

        public static bool won = false;

        public static int refreshRate;

        static void Main(string[] args)
        {
            //Song Selection

            Console.WriteLine("What is the song you want to load (\"example.ghc\") ?");

            foreach (string file in Directory.GetFiles("songs"))
            {
                if (file.EndsWith(".ghc")) {
                    Console.WriteLine("- " + file.Substring(6));
                }
            }

            string song = Console.ReadLine();

            try
            {
                File.OpenRead("songs/" + song);
            } catch (Exception e)
            {
                Console.WriteLine("This song doesn't exist. Press any key to exit");
                Console.ReadKey();
                return;
            }

            SongReader reader = new SongReader(File.OpenRead("songs/" + song));

            //CueSdk initializing

            Random rand = new Random();

            Thread updateKeys = new Thread(Input.updateKeys);
            updateKeys.SetApartmentState(ApartmentState.STA);
            updateKeys.Start();

            CorsairKeyboard keyboard;

            try {
                CueSDK.Initialize();

                Console.WriteLine("Initialized CueSDK");

                keyboard = CueSDK.KeyboardSDK;
    
                if (keyboard == null)
                {
                  Console.WriteLine("No keyboard found. Press any key to exit");
                  Console.ReadKey();
                  Environment.Exit(0);
                  return;
                }
            }catch(Exception e)
            {
                Console.WriteLine("No keyboard found. Press any key to exit");
                Console.ReadKey();
                Environment.Exit(0);
                return;
            }

            CueSDK.Reinitialize(true);

            Console.WriteLine("Found corsair keyboard [" + keyboard.DeviceInfo.Model + "]");

            Console.WriteLine("Initialized GUITARHERO Game v" + build_id);

            CorsairColor green = new CorsairColor(0, 255, 0);
            CorsairColor blue = new CorsairColor(0, 0, 255);
            CorsairColor red = new CorsairColor(255, 0, 0);
            CorsairColor yellow = new CorsairColor(255, 255, 0);
            CorsairColor orange = new CorsairColor(255, 165, 0);
            CorsairColor white = new CorsairColor(255, 255, 255);
            CorsairColor nothing = new CorsairColor(0, 0, 0);

            refreshRate = 1000 / 11;

            initLed();

            reader.init();

            Thread songthread = new Thread(reader.start);
            songthread.Start();

            while (!ended)
            {
                //Tick
                bool[] must = getMustClick();

                foreach(Note note in new List<Note>(notes))
                {
                    if(note.getX() == 0)
                    {
                        if(!(must[0] == n1 && must[1] == nq && must[2] == na && must[3] == nz))
                        {
                            health -= 0.35f;
                        }

                        break;
                    }
                }

                foreach (Note note in new List<Note>(notes))
                {
                    if (note.getX() <= 0) note.setDead();
                    if (note.isDead()) notes.Remove(note);
                }

                foreach (Note note in new List<Note>(notes))
                {
                    note.tick();
                }

                if ((int)health <= 0) ended = true;

                //Render
                clearSpace(keyboard);

                foreach (Note note in notes)
                {
                    keyboard[getLed(note.getX(), note.getY())].Color = note.getColor();
                }

                keyboard[CorsairLedId.D1].Color = white; keyboard[CorsairLedId.Q].Color = white;
                keyboard[CorsairLedId.A].Color = white; keyboard[CorsairLedId.Z].Color = white;

                for (int i = 1; i <= (int)health; i++)
                {
                    keyboard[(CorsairLedId)System.Enum.Parse(typeof(CorsairLedId), "F" + i)].Color = red;
                }

                //Post-tick
                keyboard.Update();

                n1 = false; nq = false; na = false; nz = false;

                Thread.Sleep(refreshRate);
            }

            songthread.Abort();
            updateKeys.Abort();
            reader.songt.Abort();
            reader.end();

            for (int i = 0; i <= 4; i++)
            {
                foreach (CorsairLed led in keyboard.Leds)
                {
                    if (won)
                    {
                        led.Color = green;
                    }
                    else
                    {
                        led.Color = red;
                    }
                }
                keyboard.Update();

                Thread.Sleep(250);

                foreach (CorsairLed led in keyboard.Leds)
                {
                    led.Color = nothing;
                }
                keyboard.Update();

                Thread.Sleep(250);
            }

            Environment.Exit(0);
        }

        public static void clearSpace(CorsairKeyboard keyboard)
        {
            CorsairColor nothing = new CorsairColor(0, 0, 0);

            for (char c = 'A'; c <= 'Z'; c++)
            {
                keyboard[(CorsairLedId)System.Enum.Parse(typeof(CorsairLedId), c + "")].Color = nothing;
            }

            for (int i = 1; i <= 12; i++)
            {
                keyboard[(CorsairLedId)System.Enum.Parse(typeof(CorsairLedId), "F" + i)].Color = nothing;
            }

            keyboard[CorsairKeyboardLedId.D0].Color = nothing;
            keyboard[CorsairKeyboardLedId.D1].Color = nothing;
            keyboard[CorsairKeyboardLedId.D2].Color = nothing;
            keyboard[CorsairKeyboardLedId.D3].Color = nothing;
            keyboard[CorsairKeyboardLedId.D4].Color = nothing;
            keyboard[CorsairKeyboardLedId.D5].Color = nothing;
            keyboard[CorsairKeyboardLedId.D6].Color = nothing;
            keyboard[CorsairKeyboardLedId.D7].Color = nothing;
            keyboard[CorsairKeyboardLedId.D8].Color = nothing;
            keyboard[CorsairKeyboardLedId.D9].Color = nothing;

            keyboard[CorsairKeyboardLedId.CommaAndLessThan].Color = nothing;
            keyboard[CorsairKeyboardLedId.PeriodAndBiggerThan].Color = nothing;
            keyboard[CorsairKeyboardLedId.SlashAndQuestionMark].Color = nothing;
            keyboard[CorsairKeyboardLedId.SemicolonAndColon].Color = nothing;
        }

        public static bool[] getMustClick()
        {
            bool[] must = new bool[4];
            must[0] = false;
            must[1] = false;
            must[2] = false;
            must[3] = false;

            foreach (Note note in notes)
            {
                if (note.getX() == 0) must[note.getType()] = true;
            }

            return must;
        }

        public static CorsairLedId getLed(int x, int y)
        {
            return cleds[x, y];
        }

        public static void initLed()
        {
            cleds[0, 0] = CorsairLedId.D1;
            cleds[0, 1] = CorsairLedId.Q;
            cleds[0, 2] = CorsairLedId.A;
            cleds[0, 3] = CorsairLedId.Z;

            cleds[1, 0] = CorsairLedId.D2;
            cleds[1, 1] = CorsairLedId.W;
            cleds[1, 2] = CorsairLedId.S;
            cleds[1, 3] = CorsairLedId.X;

            cleds[2, 0] = CorsairLedId.D3;
            cleds[2, 1] = CorsairLedId.E;
            cleds[2, 2] = CorsairLedId.D;
            cleds[2, 3] = CorsairLedId.C;

            cleds[3, 0] = CorsairLedId.D4;
            cleds[3, 1] = CorsairLedId.R;
            cleds[3, 2] = CorsairLedId.F;
            cleds[3, 3] = CorsairLedId.V;

            cleds[4, 0] = CorsairLedId.D5;
            cleds[4, 1] = CorsairLedId.T;
            cleds[4, 2] = CorsairLedId.G;
            cleds[4, 3] = CorsairLedId.B;

            cleds[5, 0] = CorsairLedId.D6;
            cleds[5, 1] = CorsairLedId.Y;
            cleds[5, 2] = CorsairLedId.H;
            cleds[5, 3] = CorsairLedId.N;

            cleds[6, 0] = CorsairLedId.D7;
            cleds[6, 1] = CorsairLedId.U;
            cleds[6, 2] = CorsairLedId.J;
            cleds[6, 3] = CorsairLedId.M;

            cleds[7, 0] = CorsairLedId.D8;
            cleds[7, 1] = CorsairLedId.I;
            cleds[7, 2] = CorsairLedId.K;
            cleds[7, 3] = CorsairLedId.CommaAndLessThan;

            cleds[8, 0] = CorsairLedId.D9;
            cleds[8, 1] = CorsairLedId.O;
            cleds[8, 2] = CorsairLedId.L;
            cleds[8, 3] = CorsairLedId.PeriodAndBiggerThan;

            cleds[9, 0] = CorsairLedId.D0;
            cleds[9, 1] = CorsairLedId.P;
            cleds[9, 2] = CorsairLedId.SemicolonAndColon;
            cleds[9, 3] = CorsairLedId.SlashAndQuestionMark;
        }
    }

    public class Note
    {
        public Note(int type)
        {
            this.type = type;
        }

        int type = 0;
        int x = 9;
        bool dead = false;

        public int getType()
        {
            return type;
        }

        public int getX()
        {
            return x;
        }

        public int getY()
        {
            //The type is conviniently the same as the height
            return type;
        }

        public void setDead()
        {
            dead = true;
        }

        public bool isDead()
        {
            return dead;
        }

        public CorsairColor getColor()
        {
            CorsairColor green = new CorsairColor(0, 255, 0);
            CorsairColor blue = new CorsairColor(0, 0, 255);
            CorsairColor red = new CorsairColor(255, 0, 0);
            CorsairColor yellow = new CorsairColor(255, 255, 0);
            CorsairColor orange = new CorsairColor(255, 165, 0);

            switch (type)
            {
                case 0:
                    return green;
                case 1:
                    return orange;
                case 2:
                    return yellow;
                case 3:
                    return blue;
                default:
                    break;
            }

            return orange;
        }

        public void tick()
        {
            x--;
        }
    }

    public static class Input
    {
        public static void updateKeys()
        {
            while (true)
            {
                Program.n1 = Keyboard.IsKeyDown(Key.D1);
                Program.nq = Keyboard.IsKeyDown(Key.Q);
                Program.na = Keyboard.IsKeyDown(Key.A);
                Program.nz = Keyboard.IsKeyDown(Key.Z);
            }
        }
    }

    public class SongReader{

        public Thread songt;
        FileStream fileStream;
        string audioPath;
        List<string> notes = new List<string>();
        WMPLib.WindowsMediaPlayer wplayer;

        public SongReader(FileStream fileStream)
        {
            this.fileStream = fileStream;
        }

        public void init()
        {
            string fileContents;
            using (StreamReader reader = new StreamReader(fileStream))
            {
                fileContents = reader.ReadToEnd();
            }
            string[] result = Regex.Split(fileContents, "\r\n|\r|\n");

            audioPath = result[0];

            for (int i = 1; i < result.Length; i++)
            {
                notes.Add(result[i]);
            }
        }

        public void start()
        {
            songt = new Thread(this.playSong);
            songt.Start();

            foreach(string note in notes)
            {
                NoteSpawn nSpawn = new NoteSpawn(note);
                nSpawn.spawn();
                Thread.Sleep(nSpawn.waitDelay());
            }

            Thread.Sleep(Program.refreshRate * 8);

            Program.won = true;
            Program.ended = true;
        }

        private void playSong()
        {
            Thread.Sleep(Program.refreshRate * 8);

            wplayer = new WMPLib.WindowsMediaPlayer();

            wplayer.URL = "songs/" + audioPath;
            wplayer.controls.play();
        }

        public void end()
        {
            wplayer.controls.stop();
        }
    }

    public class NoteSpawn
    {
        string note;

        public NoteSpawn(string note)
        {
            this.note = note;
        }

        public void spawn()
        {
            if(note.Split(';')[0].Equals("1")) Program.notes.Add(new Note(0));
            if (note.Split(';')[1].Equals("1")) Program.notes.Add(new Note(1));
            if (note.Split(';')[2].Equals("1")) Program.notes.Add(new Note(2));
            if (note.Split(';')[3].Equals("1")) Program.notes.Add(new Note(3));
        }

        public int waitDelay()
        {
            return Int32.Parse(note.Split(';')[4]);
        }
    }
}
