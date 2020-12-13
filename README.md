# FastTextCategory

A small program to look for the words in a certain category throughout the FastText words embedding.

## How to use
You need to have wiki-news-300d-1M.vec from https://fasttext.cc/. Put the file into the folder with built exe, or alternatively modify `_wordsFile` at the beginning of Program.cs to your location of wiki-news-300d-1M.vec.

Put some words from your sought category into a file, one word per line (see attached example*.txt files). Run the program with the first parameter being the path to the file with words, like this:

```FastText.exe exampleJobs.txt```

You can optionally specify, as a second parameter, the number of words from FastText to be searched (the given number of most frequent words is searched):

```FastText.exe exampleJobs.txt 100000```

This option defaults to 50000.

The results are then shown in the console and saved as output\_\*.csv.

The data loaded from FastText are cached into serialized\_\*.bin files for faster repeated runs. In case of using many different values as the second parameter, these files may grow somewhat large in size.
