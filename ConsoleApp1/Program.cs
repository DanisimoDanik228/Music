namespace ConsoleApp1
{

    using NAudio.Wave;
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Linq;

    public class WaveformOverlayComparer
    {
        public static void CompareAndDraw(
            string file1,
            string file2,
            string outputPath,
            int width = 1000,
            int height = 500)
        {
            // Загрузка и нормализация данных
            var (samples1, samples2) = LoadAndNormalize(file1, file2);

            // Создание изображения
            using (var bitmap = new Bitmap(width, height))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Вычисление масштабов
                float step = (float)samples1.Length / width;
                float centerY = height / 2f;
                float scale = height * 0.45f;

                // Создание путей для обеих волн
                var path1 = new System.Drawing.Drawing2D.GraphicsPath();
                var path2 = new System.Drawing.Drawing2D.GraphicsPath();

                // Построение волн
                for (int x = 0; x < width; x++)
                {
                    int start = (int)(x * step);
                    int end = (int)((x + 1) * step);
                    end = Math.Min(end, samples1.Length - 1);

                    // Находим максимальные значения в сегменте
                    float max1 = samples1.Skip(start).Take(end - start).Max();
                    float max2 = samples2.Skip(start).Take(end - start).Max();
                    float min1 = samples1.Skip(start).Take(end - start).Min();
                    float min2 = samples2.Skip(start).Take(end - start).Min();

                    float y1Top = centerY - max1 * scale;
                    float y1Bottom = centerY - min1 * scale;
                    float y2Top = centerY - max2 * scale;
                    float y2Bottom = centerY - min2 * scale;

                    path1.AddLine(x, y1Top, x, y1Bottom);
                    path2.AddLine(x, y2Top, x, y2Bottom);
                }

                // Рисуем области пересечения
                var intersect = new System.Drawing.Drawing2D.GraphicsPath();
                intersect.AddPath(path1, false);
                intersect.AddPath(path2, false);

                // Вычисляем площадь пересечения
                var region1 = new Region(path1);
                var region2 = new Region(path2);
                region1.Intersect(region2);

                // Приблизительная оценка площади пересечения
                float totalArea = width * height;
                float intersectArea = EstimateRegionArea(region1, bitmap.Size);
                float similarityPercent = (intersectArea / totalArea) * 100;

                // Рисуем результат
                g.FillRectangle(Brushes.LightGray, 0, 0, width, height);
                g.FillPath(new SolidBrush(Color.FromArgb(100, 0, 0, 255)), path1);
                g.FillPath(new SolidBrush(Color.FromArgb(100, 255, 0, 0)), path2);
                g.FillRegion(Brushes.Purple, region1);

                // Подписи
                var font = new Font("Arial", 14);
                g.DrawString($"Схожесть: {similarityPercent:F1}%", font, Brushes.Black, 20, 20);
                g.DrawString("Файл 1 (синий)", font, Brushes.Blue, 20, 50);
                g.DrawString("Файл 2 (красный)", font, Brushes.Red, 20, 80);
                g.DrawString("Область пересечения (фиолетовый)", font, Brushes.Purple, 20, 110);

                bitmap.Save(outputPath, ImageFormat.Png);
            }
        }

        private static float EstimateRegionArea(Region region, Size imageSize)
        {
            // Метод оценки площади региона через растеризацию
            using (var bmp = new Bitmap(imageSize.Width, imageSize.Height))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.FillRegion(Brushes.White, region);

                float whitePixels = 0;
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        if (bmp.GetPixel(x, y).R > 128)
                            whitePixels++;
                    }
                }
                return whitePixels;
            }
        }

        private static (float[], float[]) LoadAndNormalize(string path1, string path2)
        {
            float[] s1 = LoadSamples(path1);
            float[] s2 = LoadSamples(path2);

            // Нормализация длины
            int minLength = Math.Min(s1.Length, s2.Length);
            s1 = s1.Take(minLength).ToArray();
            s2 = s2.Take(minLength).ToArray();

            // Нормализация амплитуд
            float max1 = s1.Max(Math.Abs);
            float max2 = s2.Max(Math.Abs);
            float globalMax = Math.Max(max1, max2);

            return (
                s1.Select(x => x / globalMax).ToArray(),
                s2.Select(x => x / globalMax).ToArray()
            );
        }

        private static float[] LoadSamples(string path)
        {
            using (var reader = new AudioFileReader(path))
            {
                var buffer = new float[reader.WaveFormat.SampleRate * 2];
                var samples = new List<float>();
                int read;

                while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    samples.AddRange(buffer.Take(read));
                }
                return samples.ToArray();
            }
        }
    }

public class Mp3ToWavConverter
    {
        public static void Convert(string inputMp3Path, string outputWavPath)
        {
            // Проверка существования файла
            if (!System.IO.File.Exists(inputMp3Path))
            {
                throw new ArgumentException("MP3 файл не найден");
            }

            using (var mp3Reader = new Mp3FileReader(inputMp3Path))
            {
                // Конвертация в WAV
                WaveFileWriter.CreateWaveFile(outputWavPath, mp3Reader);
            }

            Console.WriteLine($"Конвертация завершена: {outputWavPath}");
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            Mp3ToWavConverter.Convert(@"C:\Users\Werty\Desktop\123.mp3", @"C:\Users\Werty\Desktop\123.wav");
            Mp3ToWavConverter.Convert(@"C:\Users\Werty\Desktop\321.mp3", @"C:\Users\Werty\Desktop\321.wav");

            WaveformOverlayComparer.CompareAndDraw(
                @"C:\Users\Werty\Desktop\123.wav",
                @"C:\Users\Werty\Desktop\321.wav",
                @"C:\Users\Werty\Desktop\12321a.png"
            );
        }
    }
}
