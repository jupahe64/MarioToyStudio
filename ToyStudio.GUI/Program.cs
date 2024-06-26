﻿using ToyStudio.GUI;
using EditorToolkit.Windowing;
using ToyStudio.GUI.Windows;

Console.WriteLine("Loading user settings...");
UserSettings.LoadSettings();
Console.WriteLine("Loading parameter database...");
//ParamDB.Init();
//Console.WriteLine("Loading area parameter loader...");
//ParamLoader.Load();

Console.WriteLine("Checking for imgui.ini");
if (!Path.Exists("imgui.ini"))
{
  Console.WriteLine("Creating imgui.ini...");
  File.WriteAllText("imgui.ini", File.ReadAllText(Path.Combine("res", "imgui-default.ini")));
  Console.WriteLine("Created!");
};

LevelEditorWindow window = new LevelEditorWindow();
WindowManager.Run();
