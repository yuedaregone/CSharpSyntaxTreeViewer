using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Win32;

namespace CSharpSyntaxTreeViewer
{
    public partial class MainWindow
    {
        private readonly SyntaxTreeParser _parser;
        private string? _currentFilePath;

        // 缓存颜色画笔以提高性能
        private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(0, 200, 0)); // 亮绿色，适应暗黑背景
        private static readonly SolidColorBrush BlueBrush = new(Color.FromRgb(100, 150, 255)); // 亮蓝色，适应暗黑背景

        public MainWindow()
        {
            InitializeComponent();
            _parser = new SyntaxTreeParser();
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "C# Files (*.cs)|*.cs|All Files (*.*)|*.*",
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    _currentFilePath = openFileDialog.FileName;
                    RefreshSyntaxTree();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"打开文件时出错: {ex.Message}\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshSyntaxTree();
        }

        private void RefreshSyntaxTree()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
            {
                MessageBox.Show(
                    "请先打开一个文件",
                    "提示",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                return;
            }

            try
            {
                var code = File.ReadAllText(_currentFilePath);
                var root = _parser.ParseCode(code);
                DisplaySyntaxTree(root);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"刷新语法树时出错: {ex.Message}\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void DisplaySyntaxTree(SyntaxNode root)
        {
            try
            {
                SyntaxTreeView.Items.Clear();
                var rootItem = new TreeViewItem
                {
                    Header = _parser.GetDetailedNodeInfo(root),
                    Tag = root,
                };
                SyntaxTreeView.Items.Add(rootItem);
                AddChildrenToTree(root, rootItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"显示语法树时出错: {ex.Message}\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void AddChildrenToTree(SyntaxNode parent, TreeViewItem parentItem)
        {
            try
            {
                var childAndTokenList = _parser.GetChildrenAndToken(parent);
                foreach (var child in childAndTokenList)
                {
                    if (child.IsToken)
                    {
                        // 处理Token
                        var token = child.AsToken();
                        var tokenItem = new TreeViewItem();
                        var tokenText = token.Kind().ToString();
                        if (!string.IsNullOrEmpty(token.Text))
                        {
                            tokenText += $": \"{token.Text}\"";
                        }
                        tokenItem.Header = tokenText;
                        tokenItem.Foreground = GreenBrush; // 深绿色
                        tokenItem.Tag = token;
                        parentItem.Items.Add(tokenItem);
                    }
                    else
                    {
                        // 处理子节点
                        var childNode = child.AsNode();
                        if (childNode == null)
                        {
                            continue;
                        }

                        var childItem = new TreeViewItem
                        {
                            Header = _parser.GetDetailedNodeInfo(childNode),
                            Tag = childNode,
                            // 为子节点设置深蓝色
                            Foreground = BlueBrush, // 深蓝色
                        };

                        parentItem.Items.Add(childItem);

                        // 递归添加子节点的子项
                        AddChildrenToTree(childNode, childItem);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"添加子节点时出错: {ex.Message}\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void SyntaxTreeView_SelectedItemChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e
        )
        {
            try
            {
                if (SyntaxTreeView.SelectedItem is TreeViewItem item && item.Tag != null)
                {
                    // 检查选中的是节点还是Token
                    if (item.Tag is SyntaxNode node)
                    {
                        DisplayNodeProperties(node);
                    }
                    else if (item.Tag is SyntaxToken token)
                    {
                        DisplayTokenProperties(token);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"选择节点时出错: {ex.Message}\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void DisplayNodeProperties(SyntaxNode node)
        {
            try
            {
                var properties = new List<KeyValuePair<string, string>>();

                // 使用反射获取节点的所有公共属性
                var type = node.GetType();
                var propertiesInfo = type.GetProperties(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                );

                foreach (var prop in propertiesInfo)
                {
                    try
                    {
                        // 只处理有意义的属性，跳过索引器和一些特殊属性
                        if (
                            prop.GetIndexParameters().Length > 0
                            || prop.Name == "Item"
                            || prop.Name == "RawKind"
                        )
                            continue;

                        var value = prop.GetValue(node);
                        var valueString = value?.ToString() ?? "null";

                        // 对于一些复杂类型，提供更有意义的表示
                        if (value != null && prop.PropertyType.Namespace != "System")
                        {
                            // 如果是SyntaxNode或其子类，显示其Kind
                            if (value is SyntaxNode syntaxNode)
                            {
                                valueString = $"{syntaxNode.Kind()} ({value.GetType().Name})";
                            }
                            // 如果是集合类型，显示元素数量
                            else if (value is System.Collections.ICollection collection)
                            {
                                valueString = $"Count: {collection.Count} ({value.GetType().Name})";
                            }
                            else
                            {
                                valueString = $"{valueString} ({value.GetType().Name})";
                            }
                        }

                        // 截断过长的文本
                        valueString = TruncateLongText(valueString, 50);

                        properties.Add(new KeyValuePair<string, string>(prop.Name, valueString));
                    }
                    catch (Exception ex)
                    {
                        properties.Add(
                            new KeyValuePair<string, string>(prop.Name, $"Error: {ex.Message}")
                        );
                    }
                }

                // 也添加一些常用的方法结果
                try
                {
                    properties.Add(
                        new KeyValuePair<string, string>(
                            "ToString()",
                            TruncateLongText(node.ToString(), 50)
                        )
                    );
                }
                catch (Exception ex)
                {
                    properties.Add(
                        new KeyValuePair<string, string>("ToString()", $"Error: {ex.Message}")
                    );
                }

                PropertiesDataGrid.ItemsSource = properties;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"显示节点属性时出错: {ex.Message}\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        /// <summary>
        /// 显示Token的属性信息
        /// </summary>
        /// <param name="token">要显示属性的Token</param>
        private void DisplayTokenProperties(SyntaxToken token)
        {
            try
            {
                var properties = new List<KeyValuePair<string, string>>();

                // 使用反射获取Token的所有公共属性
                var type = token.GetType();
                var propertiesInfo = type.GetProperties(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                );

                foreach (var prop in propertiesInfo)
                {
                    try
                    {
                        // 只处理有意义的属性，跳过索引器
                        if (prop.GetIndexParameters().Length > 0)
                            continue;

                        var value = prop.GetValue(token);
                        var valueString = value?.ToString() ?? "null";

                        // 对于一些复杂类型，提供更有意义的表示
                        if (
                            value != null
                            && prop.PropertyType.Namespace != "System"
                        )
                        {
                            // 如果是集合类型，显示元素数量
                            if (value is System.Collections.ICollection collection)
                            {
                                valueString = $"Count: {collection.Count} ({value.GetType().Name})";
                            }
                            // 如果是SyntaxTriviaList，显示详细信息
                            else if (value is SyntaxTriviaList triviaList)
                            {
                                valueString = $"Count: {triviaList.Count}";
                                // 添加前几个Trivia的详细信息
                                for (var i = 0; i < Math.Min(triviaList.Count, 3); i++)
                                {
                                    var trivia = triviaList[i];
                                    valueString +=
                                        $"\n  [{i}] {trivia.Kind()}: \"{TruncateLongText(trivia.ToString(), 30)}\"";
                                }
                            }
                            else
                            {
                                valueString = $"{valueString} ({value.GetType().Name})";
                            }
                        }

                        // 截断过长的文本
                        valueString = TruncateLongText(valueString, 100);

                        properties.Add(new KeyValuePair<string, string>(prop.Name, valueString));
                    }
                    catch (Exception ex)
                    {
                        properties.Add(
                            new KeyValuePair<string, string>(prop.Name, $"Error: {ex.Message}")
                        );
                    }
                }

                // 也添加一些常用的方法结果
                try
                {
                    properties.Add(
                        new KeyValuePair<string, string>(
                            "ToString()",
                            TruncateLongText(token.ToString(), 50)
                        )
                    );
                }
                catch (Exception ex)
                {
                    properties.Add(
                        new KeyValuePair<string, string>("ToString()", $"Error: {ex.Message}")
                    );
                }

                PropertiesDataGrid.ItemsSource = properties;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"显示Token属性时出错: {ex.Message}\n{ex.StackTrace}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private static readonly char[] _lineTag = ['\r', '\n'];

        /// <summary>
        /// 截断过长的文本，只保留指定长度并添加省略号
        /// </summary>
        /// <param name="text">要截断的文本</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>截断后的文本</returns>
        private string TruncateLongText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text;

            var index = text.IndexOfAny(_lineTag);
            if (index != -1)
            {
                maxLength = Math.Min(index, maxLength);
            }

            return text.Substring(0, maxLength) + "...";
        }
    }
}
