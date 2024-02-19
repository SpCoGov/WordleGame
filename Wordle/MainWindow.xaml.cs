using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Wordle;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private const int Length = 5;
    private const int Row = 6;
    private string? _targetWord;
    private const string ApiBaseUrl = "https://api.dictionaryapi.dev/api/v2/entries/en/";

    public MainWindow() {
        InitializeComponent();
        DataContext = this;

        PrepareWord();

        int tabIndex = 0;
        bool isFirst = true;
        for (int i = 0; i < Row; i++) {
            var s = new StackPanel();
            s.Orientation = Orientation.Horizontal;
            for (int i2 = 0; i2 < Length; i2++) {
                TextBox tb = new TextBox();
                tb.MaxLength = 1;
                tb.Width = 55;
                tb.Height = 55;
                tb.HorizontalContentAlignment = HorizontalAlignment.Center;
                tb.VerticalContentAlignment = VerticalAlignment.Center;
                tb.FontSize = 30;
                tb.Margin = new Thickness(5, 0, 0, 0);
                tb.IsReadOnly = !isFirst;
                tb.Focusable = isFirst;
                isFirst = false;
                tb.TabIndex = tabIndex++;

                // 案件按下事件
                tb.PreviewKeyDown += async (sender, args) => {
                    TextBox currentTextBox = (TextBox) sender;
                    // 如果当前编辑框为只读则不进行任何操作
                    if (currentTextBox.IsReadOnly) {
                        args.Handled = true;
                        return;
                    }

                    // 判断输入是否为字母
                    if (args.Key is >= Key.A and <= Key.Z) {
                        // 如果当前编辑框为空，则将编辑框的内容设置为用户输入的按键
                        bool isEmpty = currentTextBox.Text == string.Empty;
                        if (isEmpty) {
                            int position = (int) args.Key - 43;
                            char letter = (char) ('A' + position - 1);
                            currentTextBox.Text = letter.ToString();
                        }

                        // 获取下一个编辑框
                        TextBox? nextTextBox = FindNextTextBox(currentTextBox);
                        // 如果当前编辑框不是最后一个编辑框
                        if (nextTextBox != null) {
                            // 解锁并跳转到下一个编辑框、并锁定当前编辑框
                            currentTextBox.IsReadOnly = true;
                            currentTextBox.Focusable = false;
                            nextTextBox.IsReadOnly = false;
                            nextTextBox.Focusable = true;
                            nextTextBox.Focus();
                            if (!isEmpty) {
                                int position = (int) args.Key - 43;
                                char letter = (char) ('A' + position - 1);
                                nextTextBox.Text = letter.ToString();
                                nextTextBox.CaretIndex = nextTextBox.Text.Length;
                            }
                        }

                        // 或如果用户按下的是退格
                    } else if (args.Key == Key.Back) {
                        // 先判断当前编辑框是否为第一个编辑框
                        // 如果不是则先将焦点移到前一个编辑框
                        TextBox? previousTextBox = FindPreviousTextBox(currentTextBox);
                        if (previousTextBox != null) {
                            previousTextBox.IsReadOnly = false;
                            previousTextBox.Focusable = true;
                            previousTextBox.Focus();
                            currentTextBox.IsReadOnly = true;
                            currentTextBox.Focusable = false;
                        }

                        // 再判断要删除哪个编辑框里的内容
                        if (currentTextBox.Text == string.Empty) {
                            // 如果当前编辑框的内容为空，则尝试删除前一个编辑框的内容
                            if (previousTextBox != null) {
                                previousTextBox.Text = string.Empty;
                            }
                        } else {
                            // 否则删除当前编辑框的内容
                            currentTextBox.Text = string.Empty;
                        }
                    } else if (args.Key == Key.Enter) {
                        if (_targetWord == null) {
                            MessageBox.Show("Target word is null");
                            return;
                        }

                        currentTextBox.IsReadOnly = true;
                        currentTextBox.Focusable = false;
                        StackPanel? currentStackPanel = FindStackPanel(currentTextBox.TabIndex);
                        Debug.Assert(currentStackPanel != null, nameof(currentStackPanel) + " != null");

                        string word = currentStackPanel.Children.Cast<TextBox>()
                            .Aggregate("", (current, textBox) => current + textBox.Text);

                        if (word.Length != Length) {
                            currentTextBox.IsReadOnly = false;
                            currentTextBox.Focusable = true;
                            MessageBox.Show("Not enough letters");
                        } else {
                            bool isWordExist = await IsWordExist(word);
                            if (!isWordExist) {
                                currentTextBox.IsReadOnly = false;
                                currentTextBox.Focusable = true;
                                MessageBox.Show("Not in word list");
                            } else {
                                int index = 0;
                                int correctCount = 0;
                                foreach (TextBox textBox in currentStackPanel.Children) {
                                    char[] letters = _targetWord.ToCharArray();
                                    List<string> markedLetters = [];
                                    if (textBox.Text == letters[index++].ToString()) {
                                        textBox.Background = new SolidColorBrush(Color.FromRgb(83, 141, 78));
                                        correctCount++;
                                    } else if (_targetWord.Contains(textBox.Text)) {
                                        if (!markedLetters.Contains(textBox.Text)) {
                                            textBox.Background = new SolidColorBrush(Color.FromRgb(180, 159, 59));
                                            markedLetters.Add(textBox.Text);
                                        } else {
                                            textBox.Background = new SolidColorBrush(Color.FromRgb(58, 58, 60));
                                        }
                                    } else {
                                        textBox.Background = new SolidColorBrush(Color.FromRgb(58, 58, 60));
                                    }
                                }

                                if (correctCount == Length) {
                                    MessageBox.Show("Win");
                                } else {
                                    StackPanel? nextStackPanel = FindNextStackPanel(currentTextBox.TabIndex);
                                    if (nextStackPanel != null) {
                                        TextBox nextTextBox = (TextBox) nextStackPanel.Children[0];
                                        nextTextBox.IsReadOnly = false;
                                        nextTextBox.Focusable = true;
                                        nextTextBox.Focus();
                                    } else {
                                        MessageBox.Show("Lose: " + _targetWord.ToLower());
                                    }
                                }
                            }
                        }
                    }

                    // 确保光标的位置在最后
                    currentTextBox.CaretIndex = currentTextBox.Text.Length;
                    args.Handled = true;
                };
                tb.TextChanged += TextBox_TextChanged;

                s.Children.Add(tb);
                s.Margin = new Thickness(0, 5, 0, 0);
            }

            tabIndex += 100000;

            AlphabetCollection.Children.Add(s);
        }
    }

    private async void PrepareWord() {
        do {
            _targetWord = await GenerateRandomWord();
        } while (!await IsWordExist(_targetWord));

        Console.WriteLine($"target word: {_targetWord}");
        MessageBox.Show("Target word is ready.\n\nGame start!");
    }

    private static async Task<string?> GenerateRandomWord() {
        try {
            // 用换行符逐行读取本地文件中的单词并筛选
            using (var reader = new StreamReader("words_alpha.txt")) {
                List<string> validWords = new List<string>();

                while (!reader.EndOfStream) {
                    string word = reader.ReadLine();

                    // 避免空引用异常
                    if (word != null && word.Length == Length) {
                        validWords.Add(word);
                    }
                }

                // 随机选择一个符合条件的单词
                if (validWords.Count > 0) {
                    return validWords[new Random().Next(0, validWords.Count)].ToUpper();
                }
            }

            MessageBox.Show($"Error: No word of length {Length} found", "Error occurred", MessageBoxButton.OK,
                MessageBoxImage.Error);
            // 找不到符合条件的单词，返回null
            return null;
        } catch (Exception ex) {
            MessageBox.Show($"Exception: {ex.Message}", "Exception occurred", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return null;
        }
    }

    private StackPanel? FindStackPanel(int tabIndex) {
        foreach (StackPanel stackPanel in AlphabetCollection.Children) {
            foreach (UIElement element in stackPanel.Children) {
                if (element is TextBox tb && tb.TabIndex == tabIndex - 1) {
                    return stackPanel;
                }
            }
        }

        return null;
    }

    private StackPanel? FindNextStackPanel(int tabIndex) {
        bool found = false;
        foreach (StackPanel stackPanel in AlphabetCollection.Children) {
            if (found) {
                return stackPanel;
            }

            foreach (UIElement element in stackPanel.Children) {
                if (element is TextBox tb && tb.TabIndex == tabIndex - 1) {
                    found = true;
                    break;
                }
            }
        }

        return null;
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
        TextBox currentTextBox = (TextBox) sender;

        if (!Regex.IsMatch(currentTextBox.Text, "^[a-zA-Z]+$")) {
            // 如果不是字母，则清空输入
            currentTextBox.Text = string.Empty;
        }
    }

    public async Task<bool> IsWordExist(string? word) {
        try {
            if (word == null) {
                return false;
            }

            using (HttpClient client = new HttpClient()) {
                string apiUrl = ApiBaseUrl + word;
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode) {
                    // 如果状态码为 200 OK，则表示单词存在
                    return true;
                } else if (response.StatusCode == HttpStatusCode.NotFound) {
                    // 如果状态码为 404 Not Found，则表示单词不存在
                    return false;
                } else {
                    // 处理其他状态码
                    MessageBox.Show($"Error: {response.StatusCode} - {response.ReasonPhrase}", "Error occurred",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        } catch (Exception ex) {
            MessageBox.Show($"Exception: {ex.Message}", "Exception occurred", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private TextBox? FindPreviousTextBox(Control currentTextBox) {
        int currentIndex = currentTextBox.TabIndex;

        foreach (StackPanel stackPanel in AlphabetCollection.Children) {
            foreach (UIElement element in stackPanel.Children) {
                if (element is TextBox tb && tb.TabIndex == currentIndex - 1) {
                    return tb;
                }
            }
        }

        return null;
    }

    private TextBox? FindNextTextBox(Control currentTextBox) {
        int currentIndex = currentTextBox.TabIndex;

        foreach (StackPanel stackPanel in AlphabetCollection.Children) {
            foreach (UIElement element in stackPanel.Children) {
                if (element is TextBox tb && tb.TabIndex == currentIndex + 1) {
                    return tb;
                }
            }
        }

        return null;
    }
}