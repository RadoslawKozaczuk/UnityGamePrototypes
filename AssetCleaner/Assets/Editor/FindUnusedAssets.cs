﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
	public class FindUnusedAssets : EditorWindow
	{
		public const string ComponentsDir = "Assets/Components";
		const string ResourcesDir = "Assets/Resources";

		enum SearchDirectories
		{
			Components, Resources
		}
		SearchDirectories _chosenDirectory = SearchDirectories.Components;

		AssetCollector _collection = new AssetCollector();
		List<DeleteAsset> _deleteAssets = new List<DeleteAsset>();
		Vector2 _scroll;
		bool _deleteAll = true;
		bool _deleteAllHasChanged;
		bool _saveEditorExtensions;

		//[MenuItem("Assets/Delete Unused Assets", false, 50)]
		//static void TheOnlyWay()
		//{
		//	var window = CreateInstance<FindUnusedAssets>();
		//	window.Show();
		//}

		[MenuItem("Assets/Delete Unused Assets/unused by editor", false, 51)]
		static void InitWithout()
		{
			var window = CreateInstance<FindUnusedAssets>();
			window._collection.Collection(ComponentsDir, true);
			window.CopyDeleteFileList(window._collection.DeleteFileList);

			window.Show();
		}

		[MenuItem("Assets/Delete Unused Assets/unused by game", false, 52)]
		static void Init()
		{
			var window = CreateInstance<FindUnusedAssets>();
			window._collection.Collection(ComponentsDir, false);
			window.CopyDeleteFileList(window._collection.DeleteFileList);

			window.Show();
		}

		// similar to any Update function.
		// Except it is not called once per frame but it is called one or more times per interaction.
		// So whenever we click or move the mouse or press a button etc.
		void OnGUI()
		{
			//using (var horizontal = new EditorGUILayout.HorizontalScope("box"))
			//{

			//}

			EditorGUILayout.BeginHorizontal();
			_saveEditorExtensions = EditorGUILayout.Toggle("Exclude objects that reference from scenes", _saveEditorExtensions);
			_chosenDirectory = (SearchDirectories)EditorGUILayout.EnumPopup(_chosenDirectory);
			EditorGUILayout.EndHorizontal();

			/*
				In Editor scripting, you will see functions which begin with 'Begin' or 'End'.
				You may treat these similarly to curly braces (except no compiler error will be thrown
				if you forget the 'End' function).
			*/

			// this is layout
			// this is equivalent to begin and end functions
			using (var horizontal = new EditorGUILayout.HorizontalScope("box"))
			{
				EditorGUILayout.LabelField("delete unreference assets from buildsettings and resources");

				// if we do click the button in the update cycle it will return true
				if (GUILayout.Button("Delete", GUILayout.Width(120), GUILayout.Height(40)) && _deleteAssets.Count != 0)
				{
					RemoveFiles();
					Close();
				}
			}

			using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
			{
				_scroll = scrollScope.scrollPosition;

				var everyFrameValue = EditorGUILayout.Toggle("select all", _deleteAll);
				if (everyFrameValue != _deleteAll)
					_deleteAllHasChanged = true;
				_deleteAll = everyFrameValue;

				foreach (var asset in _deleteAssets)
				{
					if (string.IsNullOrEmpty(asset.Path))
						continue;

					using (var horizontal = new EditorGUILayout.HorizontalScope())
					{
						if (_deleteAllHasChanged)
							asset.IsDelete = _deleteAll;

						asset.IsDelete = EditorGUILayout.Toggle(asset.IsDelete, GUILayout.Width(20));

						var icon = AssetDatabase.GetCachedIcon(asset.Path);
						GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
						if (GUILayout.Button(asset.Path, EditorStyles.largeLabel))
							Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(asset.Path);
					}
				}
			}
		}

		static void CleanDir()
		{
			RemoveEmptyDirectory("Assets");
			AssetDatabase.Refresh();
		}

		void CopyDeleteFileList(IEnumerable<string> deleteFileList)
		{
			foreach (var asset in deleteFileList)
			{
				var filePath = AssetDatabase.GUIDToAssetPath(asset);
				if (string.IsNullOrEmpty(filePath) == false)
					_deleteAssets.Add(new DeleteAsset { Path = filePath });
			}
		}

		void RemoveFiles()
		{
			try
			{
				string exportDirectry = "BackupUnusedAssets";
				Directory.CreateDirectory(exportDirectry);
				var files = _deleteAssets.Where(item => item.IsDelete).Select(item => item.Path).ToArray();
				string backupPackageName = exportDirectry + "/package" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".unitypackage";
				EditorUtility.DisplayProgressBar("export package", backupPackageName, 0);
				AssetDatabase.ExportPackage(files, backupPackageName);

				int i = 0;
				int length = _deleteAssets.Count;

				foreach (var assetPath in files)
				{
					i++;
					EditorUtility.DisplayProgressBar("delete unused assets", assetPath, (float)i / length);
					AssetDatabase.DeleteAsset(assetPath);
				}

				EditorUtility.DisplayProgressBar("clean directory", "", 1);
				foreach (var dir in Directory.GetDirectories("Assets"))
					RemoveEmptyDirectory(dir);

				System.Diagnostics.Process.Start(exportDirectry);

				AssetDatabase.Refresh();
			}
			catch (System.Exception e)
			{
				Debug.Log(e.Message);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		static void RemoveEmptyDirectory(string path)
		{
			foreach (var dir in Directory.GetDirectories(path))
				RemoveEmptyDirectory(dir);

			var files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Where(item => Path.GetExtension(item) != ".meta");
			if (files.Count() == 0 && Directory.GetDirectories(path).Length == 0)
			{
				var metaFile = AssetDatabase.GetTextMetaFilePathFromAssetPath(path);
				FileUtil.DeleteFileOrDirectory(path);
				FileUtil.DeleteFileOrDirectory(metaFile);
			}
		}

		class DeleteAsset
		{
			public bool IsDelete = true;
			public string Path;
		}
	}
}
