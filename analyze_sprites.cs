using System;
using System.Drawing;
using System.IO;

class Program {
    static void Main(string[] args) {
        string[] files = { "walk-r.png", "walk-l.png", "sprint-r.png", "sprint-l.png" };
        foreach(var f in files) {
            string path = Path.Combine("Assets/Sprites/Characters", f);
            if(!File.Exists(path)) continue;
            using (Bitmap bmp = new Bitmap(path)) {
                int frameWidth = bmp.Width / 4;
                Console.WriteLine(f + ":");
                for(int i=0; i<4; i++) {
                    int minX = frameWidth, maxX = 0;
                    for(int x=0; x<frameWidth; x++) {
                        for(int y=0; y<bmp.Height; y++) {
                            if(bmp.GetPixel(i*frameWidth + x, y).A > 10) {
                                if(x < minX) minX = x;
                                if(x > maxX) maxX = x;
                            }
                        }
                    }
                    Console.WriteLine("  Frame " + i + " visible width: " + (maxX - minX));
                }
            }
        }
    }
}
