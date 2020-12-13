using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace FastTextCategory
{
    class Program
    {
        private const string _wordsFile = "wiki-news-300d-1M.vec";

        private const string _serializedFile = "serialized_";
        private const string _serializedFileExt = ".bin";

        private const int _outCount = 100;
        private const int _defaultVecsCount = 50000;

        private static Dictionary<string, float[]> Terms { get; set; }

        private static uint _vectorsCount = _defaultVecsCount;

        private static uint _capacity;
        private static int _dimension = 300;

        private static string _inputFile;
        private static string[] _inputWords;

        private static double CountTime(Action action)
        {
            var sw = new Stopwatch();
            sw.Start();

            action();

            sw.Stop();
            return sw.Elapsed.TotalSeconds;
        }

        private static void LoadVectorsFromSource()
        {
            if (!File.Exists(_wordsFile))
                throw new Exception($"The FastText file \"{_wordsFile}\" wasn't found in the exe directory.");

            using (var reader = new StreamReader(_wordsFile))
            {
                var line = reader.ReadLine();

                var splits = line.Split(' ');

                var wordsCount = uint.Parse(splits[0]);
                _capacity = _vectorsCount == 0 ? wordsCount : Math.Min(wordsCount, _vectorsCount);

                _dimension = int.Parse(splits[1]);

                Terms = ReadTerms(reader);
            }
        }

        private static Dictionary<string, float[]> ReadTerms(StreamReader reader)
        {
            var result = new Dictionary<string, float[]>();

            for (var i = 0; i < _capacity; i++)
            {
                var line = reader.ReadLine();
                var splits = line.Split(' ');

                var vector = new float[_dimension];

                for (var j = 1; j < splits.Length; j++)
                {
                    vector[j - 1] = float.Parse(splits[j]);
                }

                result[splits[0]] = vector;
            }

            return result;
        }

        private static void SerializeVectors()
        {
            IFormatter formatter = new BinaryFormatter();

            using (var stream = new FileStream($"{_serializedFile}{_vectorsCount}{_serializedFileExt}", FileMode.Create,
                FileAccess.Write, FileShare.None))
            {
                formatter.Serialize(stream, Terms);
            }
        }

        private static void LoadSerializedVectors()
        {
            IFormatter formatter = new BinaryFormatter();
            using (var stream = new FileStream($"{_serializedFile}{_vectorsCount}{_serializedFileExt}", FileMode.Open,
                FileAccess.Read, FileShare.Read))
            {
                Terms = (Dictionary<string, float[]>) formatter.Deserialize(stream);
            }
        }


        private static double Distance(string word1, string word2)
        {
            return Distance(Terms[word1], Terms[word2]);
        }

        private static double Distance(float[] vector1, float[] vector2)
        {
            var distance = Math.Sqrt(vector1.Zip(vector2, (x, y) => ((double) x - y) * ((double) x - y)).Sum());

            return distance;
        }

        private static float[] Middle(params string[] terms)
        {
            var vectors = terms.Select(t => Terms[t]).ToArray();

            return Middle(vectors);
        }

        private static float[] Middle(float[][] vectors)
        {
            var middle = Enumerable.Range(0, vectors[0].Length)
                .Select(i => vectors.Sum(v => v[i]))
                .ToArray();

            return middle;
        }

        private static IEnumerable<Tuple<string, double>> NearestNeighbors(string term)
        {
            return NearestNeighbors(Terms[term]);
        }

        private static IEnumerable<Tuple<string, double>> NearestNeighbors(float[] vector)
        {
            var result = Terms.Select(t => new Tuple<string, double>(t.Key, Distance(vector, t.Value)))
                .OrderBy(t => t.Item2);

            return result;
        }

        private static IEnumerable<Tuple<string, double>> NearestNeighborsSet(out string[] unusedWords,
            params string[] terms)
        {
            var containedTerms = terms.Where(Terms.ContainsKey).ToArray();

            if (containedTerms.Length == 0)
            {
                throw new Exception("None of the words was found in the FastText dictionary.");
            }

            var vectors = containedTerms.Select(t => Terms[t]).ToArray();

            unusedWords = terms.Except(containedTerms).ToArray();

            return NearestNeighborsSet(vectors).Where(wd => terms.All(t => t != wd.Item1));
        }

        private static IEnumerable<Tuple<string, double>> NearestNeighborsSet(float[][] vectors)
        {
            var middle = Middle(vectors);

            var result = NearestNeighbors(middle);

            return result;
        }

        private static float[] Direction(string from, string to)
        {
            return Direction(Terms[from], Terms[to]);
        }

        private static float[] Direction(float[] from, float[] to)
        {
            var vector = to.Select((x, i) => x - from[i]).ToArray();

            return vector;
        }

        static void Main(string[] args)
        {
            try
            {
                ReadParameters(args);

                ReadInput();

                LoadVectors();

                //AnalogiesExamples();
                
                var nns = NearestNeighborsSet(out var unusedWords, _inputWords);

                var usedWordsCount = _inputWords.Length - unusedWords.Length;

                Console.WriteLine(
                    $"{usedWordsCount} out of {_inputWords.Length} input words found in FastText dictionary.");

                if (unusedWords.Any())
                {
                    Console.WriteLine($"Words not found: {string.Join(", ", unusedWords)}");
                }

                WriteOutput(nns);
            }
            catch (Exception e)
            {
                Console.WriteLine();
                Console.WriteLine($"ERROR: {e.Message}");
                Console.WriteLine();
                WriteHelp();
            }

            Console.ReadLine();
        }

        private static void ReadParameters(string[] args)
        {
            if (args.Length == 0)
            {
                throw new Exception("Missing argument: inputFile");
            }

            _inputFile = args[0];

            if (!File.Exists(_inputFile))
            {
                throw new Exception($"Input file \"{_inputFile}\" doesn't exist.");
            }

            if (args.Length > 1 && uint.TryParse(args[1], out var arg2))
            {
                _vectorsCount = arg2;
            }
        }

        private static void WriteHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  FastText.exe input [wordsCount]");
            Console.WriteLine("  input:");
            Console.WriteLine("    file with one input word per line (with no headline)");
            Console.WriteLine("  wordsCount (optional):");
            Console.WriteLine(
                $"    number of word-vectors from the beginning of the FastText file used. Default {_defaultVecsCount}, for all the words use 0");
            Console.WriteLine("Examples:");
            Console.WriteLine("    FastText.exe inputFile.txt 100000");
            Console.WriteLine("    FastText.exe inputFile.txt");
            Console.WriteLine(
                $"The first {_outCount} nearest neighbours of input words (with their distances) will be written to output_{{n}}.csv");
        }

        private static void ReadInput()
        {
            _inputWords = File.ReadAllLines(_inputFile).Select(w => w.Trim(';', ',', ' ')).ToArray();

            Console.WriteLine($"Read {_inputWords.Length} words from '{_inputFile}'.");
        }

        private static void LoadVectors()
        {
            var cacheName = $"{_serializedFile}{_vectorsCount}{_serializedFileExt}";

            var cacheExists = File.Exists(cacheName);
            var cacheMode = _vectorsCount > 0 && _vectorsCount < 500000;

            if (cacheMode && cacheExists)
            {
                var secs = CountTime(LoadSerializedVectors);
                Console.WriteLine($"Deserialized {Terms.Count} words from \"{cacheName}\" in {secs} seconds.");
            }
            else
            {
                var secs = CountTime(LoadVectorsFromSource);
                Console.WriteLine($"Loaded {Terms.Count} words from \"{_wordsFile}\" in {secs} seconds.");

                if (cacheMode)
                {
                    secs = CountTime(SerializeVectors);
                    Console.WriteLine($"Serialized {Terms.Count} words into \"{cacheName}\" in {secs} seconds.");
                }
            }
        }

        private static void WriteOutput(IEnumerable<Tuple<string, double>> words)
        {
            var actualWords = words.Take(_outCount).ToArray();

            var path = GetOutPath();
            File.WriteAllLines(path,
                actualWords.Select(t => $"{t.Item1}, {t.Item2.ToString(CultureInfo.InvariantCulture)}"));

            Console.WriteLine($"Written {actualWords.Length} output words to \"{path}\".");
            Console.WriteLine();
            Console.WriteLine($"Words: {string.Join(", ", actualWords.Select(t => t.Item1))}");
        }

        private static string GetOutPath()
        {
            var i = 0;
            string path;

            do
            {
                path = $"output_{i}.csv";
                i++;
            } while (File.Exists(path));

            return path;
        }

        private static void AnalogiesExamples()
        {
            EchoAnalogy("sky", "blue", "grass");
            EchoAnalogy("soccer", "ball", "hockey");
            EchoAnalogy("cabbage", "vegetable", "apple");
            EchoAnalogy("citizen", "state", "student");
            EchoAnalogy("programmer", "code", "plumber");
        }

        private static IEnumerable<Tuple<string, double>> Analogy(string from1, string to1, string from2)
        {
            var dir = Direction(from1, to1);

            var point = Terms[from2].Zip(dir, (a, b) => a + b).ToArray();

            var result = NearestNeighbors(point);

            return result;
        }

        private static void EchoAnalogy(string from1, string to1, string from2)
        {
            var analogies = Analogy(from1, to1, from2).Take(_outCount);
            Console.WriteLine($"{from1} to {to1} is like {from2} to:");
            Console.WriteLine(string.Join(", ", analogies.Select(a => $"{a.Item1} ({a.Item2:N2})")));
            Console.WriteLine();
        }
    }

    public class Term
    {
        public string Word { get; set; }

        public List<float> Vector { get; set; }
    }
}