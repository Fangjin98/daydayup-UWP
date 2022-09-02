﻿using DayDayUp.Helpers;
using DayDayUp.Models;
using DayDayUp.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Nito.AsyncEx;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace DayDayUp.ViewModels
{
    public class DashboardPageViewModel : BaseTodoListViewModel
    {
        private double doingTaskBias;

        private double finishedTaskBias;

        public List<ISeries> LineChartHistory { get; set; }

        public ObservableCollection<ISeries> PieStatics { get; set; }

        public IEnumerable<ICartesianAxis> XAxes { get; set; }

        public IEnumerable<ICartesianAxis> YAxes { get; set; }

        public int DoingTaskCount { get; set; }

        public int FinishedTaskCount { get; set; }

        public double DoingTaskBias
        {
            get => doingTaskBias;
            set => SetProperty(ref doingTaskBias, value);
        }

        public double FinishedTaskBias
        {
            get => finishedTaskBias;
            set => SetProperty(ref finishedTaskBias, value);
        }

        public double TotalBias
        {
            get => DoingTaskBias + FinishedTaskBias;
        }

        public List<string> Categories { get; } = new List<string>()
        {
            "Progress","Duration","Creation date"
        };

        public DashboardPageViewModel(TodoManager TodoManager):
            base(TodoManager)
        {
            FinishedTaskCount = todoManager.FinishedTodos.Count;
            DoingTaskCount = todoManager.UnfinishedTodos.Count;

            initCharts();
        }

        public void SetStatics(string categoryName)
        {
            switch (categoryName)
            {
                case "Progress":
                    PieStatics.Clear();
                    foreach (var item in progress.Where(p => p.Count != 0))
                    {
                        PieStatics.Add(
                            new PieSeries<int> { Values = new int[] { item.Count }, Name = item.Name, 
                                Fill=color[progress.IndexOf(item)],
                                InnerRadius = 30
                            }
                            );
                    }
                    break;
                case "Duration":
                    PieStatics.Clear();
                    foreach (var item in duration.Where(p => p.Count != 0))
                    {
                        PieStatics.Add(
                            new PieSeries<int> { Values = new int[] { item.Count }, Name = item.Name,
                                Fill = color[duration.IndexOf(item)],
                                InnerRadius = 30
                            }
                            );
                    }
                    break;
                case "Creation date":
                    PieStatics.Clear();
                    foreach (var item in creationDate.Where(p => p.Count != 0))
                    {
                        PieStatics.Add(
                            new PieSeries<int> { Values = new int[] { item.Count }, 
                                Name = item.Name, 
                                Fill= color[creationDate.IndexOf(item)], 
                                InnerRadius = 30 }
                            );
                    }
                    break;
            }
        }

        private void initCharts()
        {
            LineChartHistory = new();
            LineChartHistory.Add(new ColumnSeries<ObservableValue>
            {
                Name = "Finished Tasks",
                Fill = new LinearGradientPaint(
                     new[] { new SKColor(255, 140, 148), new SKColor(220, 237, 194) }
                     ),
                Values = historyCount,
                Stroke = null,
                ScalesYAt = 0
            });
            LineChartHistory.Add(
                new LineSeries<ObservableValue>
                {
                    Name = "Estimated Bias",
                    Values = historyBias,
                    GeometrySize = 6,
                    GeometryStroke = new LinearGradientPaint(gradientColors.ToArray()) { StrokeThickness = 4 },
                    //Stroke=new SolidColorPaint(SkiaSharp.SKColor.Parse(Application.Current.Resources["LineSeriesColor"].ToString())) { StrokeThickness=4},
                    Stroke = new LinearGradientPaint(gradientColors.ToArray()) { StrokeThickness = 4},
                    ScalesYAt = 1,
                    Fill = null
                }
                );

            PieStatics = new();

            xLabel.Add("Today");
            xLabel.Add("Yesterday");
            for (int i = 2; i < 7; i++)
            {
                xLabel.Add(DateTime.Now.AddDays(-i).ToString("M"));
            }

            XAxes = new Axis[]
            {
                new Axis{
                    Labels = xLabel,
                }
            };

            YAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Count",
                    NamePaint = new SolidColorPaint(SkiaSharp.SKColor.Parse(Application.Current.Resources["NameTextBrush"].ToString())),
                    MinLimit = 0,
                    MaxLimit = 4,
                    ForceStepToMin = true,
                    MinStep = 1,
                    NameTextSize = 14,
                    TextSize = 12,
                },
                new Axis
                {
                    Name = "Bias Ratio",
                    NamePaint = new SolidColorPaint(SkiaSharp.SKColor.Parse(Application.Current.Resources["NameTextBrush"].ToString())),
                    MinLimit = -2,
                    MaxLimit = 2,
                    ForceStepToMin = true,
                    MinStep = 1,
                    NameTextSize = 14,
                    TextSize = 12,
                    ShowSeparatorLines = false,
                    Position = LiveChartsCore.Measure.AxisPosition.End
                }
            };
        }

        private void updateHistory(Todo item)
        {
            var diffHours = Convert.ToInt32(DateTime.Now.Subtract(item.TimeStamps.Last()).TotalHours);
            if (diffHours < (24 - item.TimeStamps.Last().Hour)) //Today
            {
                historyBias[0].Value += item.Bias;
                historyCount[0].Value++;
            }
            else
            {
                var diffDays=1+diffHours / 24;
                if (diffDays < 7)
                {
                    historyCount[diffDays].Value++;
                    if (item.ExpectedDurationMins != 0)
                    {
                        historyBias[diffDays].Value += item.Bias;
                    }
                }
            }
        }

        private void updateStatics(List<Todo> todos)
        {
            progress[0].Count=todos.Count(t =>(t.Progress == 0 || t.ExpectedDurationMins==0));
            progress[1].Count=todos.Count(t=>(t.Progress > 0 && t.Progress<=50));
            progress[2].Count = todos.Count(t => (t.Progress>50 && t.Progress <= 100));
            progress[3].Count = todos.Count(t => t.Progress > 100);

            duration[0].Count = todos.Count(t => t.DurationMins < 60);
            duration[1].Count = todos.Count(t => (t.DurationMins >= 60 && t.DurationMins < 60 * 12));
            duration[2].Count = todos.Count(t => (t.DurationMins >= 60 * 12 && t.DurationMins < 60 * 24));
            duration[3].Count = todos.Count(t => t.DurationMins >= 60 * 24);

            creationDate[0].Count=todos.Count(t => (DateTime.Now-t.CreationDate).TotalMinutes<5);
            creationDate[1].Count = todos.Count(t => (DateTime.Now - t.CreationDate).TotalHours <= 1)
                - creationDate[0].Count;
            creationDate[2].Count = todos.Count(t => (DateTime.Now -t.CreationDate).TotalDays <= 1)
                - creationDate[0].Count-creationDate[1].Count;
            creationDate[3].Count = todos.Count(t => (DateTime.Now - t.CreationDate).TotalDays <= 7)
                - creationDate[0].Count - creationDate[1].Count - creationDate[2].Count;
            creationDate[4].Count = todos.Count(t => (DateTime.Now - t.CreationDate).TotalDays > 7);
        }

        protected override async Task LoadTodoAsync()
        {
            using (await loadingLock.LockAsync())
            {

                foreach (var item in todoManager.FinishedTodos)
                {
                    FinishedTaskBias += item.Bias;
                    updateHistory(item);
                }

                foreach (var item in todoManager.UnfinishedTodos)
                {
                    DoingTaskBias += item.Bias;
                }

                updateStatics(todoManager.UnfinishedTodos);

                SetStatics("Progress");
            }
        }

        private List<string> xLabel = new List<string>();

        private ObservableValue[] historyCount = new ObservableValue[]
       {
            new ObservableValue(0),
            new ObservableValue(0),
            new ObservableValue(0),
            new ObservableValue(0),
            new ObservableValue(0),
            new ObservableValue(0),
            new ObservableValue(0)
       };

        private ObservableValue[] historyBias = new ObservableValue[]
       {
            new ObservableValue(0),
            new ObservableValue(0),
            new ObservableValue(0),
            new ObservableValue(0),
            new ObservableValue(0),
            new ObservableValue(0),
            new ObservableValue(0)
       };

        private List<DoingStatics> progress = new()
        {
            new DoingStatics { Name = "Not started", Count = 0 },
            new DoingStatics { Name = "For a while", Count = 0 },
            new DoingStatics { Name = "Almost due", Count = 0 },
            new DoingStatics { Name = "Overdue", Count = 0 }
        };
        private List<DoingStatics> duration = new()
        {
            new DoingStatics { Name = "< 1 hour", Count = 0 },
            new DoingStatics { Name = "Half days", Count = 0 },
            new DoingStatics { Name = "One day", Count = 0 },
            new DoingStatics { Name = "Over one day", Count = 0 }
        };
        private List<DoingStatics> creationDate = new()
        {
            new DoingStatics { Name = "Now", Count = 0 },
            new DoingStatics { Name = "An hour ago", Count = 0 },
            new DoingStatics { Name = "Today", Count = 0 },
            new DoingStatics { Name = "This week", Count = 0 },
            new DoingStatics { Name = "A week ago", Count = 0 }
        };
        private List<SolidColorPaint> color = new()
        {
            new SolidColorPaint(SkiaSharp.SKColor.Parse(Application.Current.Resources["PieChartColor0"].ToString())),
            new SolidColorPaint(SkiaSharp.SKColor.Parse(Application.Current.Resources["PieChartColor1"].ToString())),
            new SolidColorPaint(SkiaSharp.SKColor.Parse(Application.Current.Resources["PieChartColor2"].ToString())),
            new SolidColorPaint(SkiaSharp.SKColor.Parse(Application.Current.Resources["PieChartColor3"].ToString()))
        };
        private List<SKColor> gradientColors = new()
        {
                new SKColor(45, 64, 89),
                new SKColor(255, 212, 96)
            };
    }

    public class DoingStatics
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

}
