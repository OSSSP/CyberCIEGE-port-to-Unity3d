﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
using System.Xml.Linq;
using System.Xml;
using System.Linq;
public class UserBehavior : MonoBehaviour {
	public static Dictionary<string, UserBehavior> user_dict = new Dictionary<string, UserBehavior>();
	static string USERS = "users";
	private static Rect WindowRect = new Rect(10, 10, 250, 300);
	public static Texture2D background, LOGO;

	string filePath;
	public string user_name;
	public int position = -1;
    public string department = null;
	public string current_thought = "";
	public int training = 0;
	public List<string> failed_goals = new List<string>();
	public static UserBehavior current_user=null;

	public static UserBehavior GetNextUser()
	{
		UserBehavior first_user = null;
		bool gotit = false;
		foreach (KeyValuePair<string, UserBehavior> entry in user_dict)
		{
			UserBehavior user = entry.Value;
			if (!user.IsActiveUser())
				continue;
			if (gotit)
			{
				current_user = user;
				return user;
			}
			if (first_user == null)
				first_user = user;
			if (current_user == null)
			{
				current_user = user;
				return user;
			}
			if (user == current_user)
				gotit = true;
		}
		current_user = first_user;
		return first_user;
	}
	public bool IsActiveUser()
	{
		if (this.department != "Security" && this.department != "Tech")
			return true;
		return false;
	}
	public static void LoadOneUser(string user_file)
	{
		GameObject user = GameObject.Find("User");
		//Debug.Log("user_app_path" + user_app_path + " file [" + User_file+"]");
		string cfile = System.IO.Path.Combine(GameLoadBehavior.user_app_path, user_file);
		//Debug.Log("user " + cdir);
		GameObject new_c = Instantiate(user, new Vector3(1.0F, 0, 0), Quaternion.identity);
		UserBehavior script = (UserBehavior)new_c.GetComponent(typeof(UserBehavior));
		script.SetFilePath(cfile);
		new_c.SetActive(true);
		script.LoadUser();
		int pos = script.position;
		//Debug.Log("LoadUsers " + script.user_name + " pos is " + pos);
		if (pos < 0)
		{
			Debug.Log("LoadOneUser got invalid pos for " + script.user_name);
			return;
		}
		if (pos >= 0)
		{
			WorkSpaceScript.WorkSpace ws = WorkSpaceScript.GetWorkSpace(pos);
			if (ws == null)
			{
				Debug.Log("UserBehavior got null workspace for pos" + pos);
				return;
			}
			if (!ws.AddUser(script.user_name))
			{
				Debug.Log("UserBehavior AddUser, could not user, already populated " + script.user_name);
				return;
			}
			float xf, zf;
			ccUtils.GridTo3dPos(ws.x, ws.y, out xf, out zf);
			//Debug.Log(ws.x + " " + ws.y + " " + xf + " " + zf);
			Vector3 v = new Vector3(xf - 1.0f, 0.5f, zf);
			new_c.transform.position = v;
		}
		else
		{
			Debug.Log("no postion for " + script.user_name);
		}
	}
	public static void LoadUsers()
	{
		string user_dir = System.IO.Path.Combine(GameLoadBehavior.user_app_path, USERS);
		string[] clist = System.IO.Directory.GetFiles(user_dir);
		foreach (string user_file in clist)
		{
			if (user_file.EndsWith(".sdf"))
			{
				LoadOneUser(user_file);
			}
		}

	}
	public static void UpdateStatus(string message)
	{

		StringReader xmlreader = new StringReader(message);
		//xmlreader.Read(); // skip BOM ???

		XmlDocument xml_doc = new XmlDocument();
		//Debug.Log("UserBehavior UpdateStatus xml is " + message);

		xml_doc.Load(xmlreader);
		XmlNodeList user_nodes = xml_doc.SelectNodes("//user_status/user");
		foreach (XmlNode user in user_nodes)
		{
			string user_name = user["name"].InnerText;
			//Debug.Log("the name is " + user_name);
			if (!user_dict.ContainsKey(user_name))
			{
				//Debug.Log("UserBehavior name not in dictionary " + user_name);
				continue;
			}
			UserBehavior user_script = user_dict[user_name];
			user_script.UpdateUserStatus(user);

		}
	}
	public void UpdateUserStatus(XmlNode user)
	{
		this.failed_goals.Clear();
		this.current_thought = user["thought"].InnerText;
		//Debug.Log("thought for user " + this.user_name + " is " + this.current_thought);
		XmlNodeList goal_nodes = user.SelectNodes("//goal");
		foreach (XmlNode goal in goal_nodes)
		{
			string status = goal["status"].InnerText;
			if (status == "fail")
			{
				this.failed_goals.Add(goal["name"].InnerText);
			}
		}
		string tmp = user["training"].InnerText;
		if (!int.TryParse(tmp, out this.training))
		{
			Debug.Log("Error: UserBehavior could not parse training " + tmp);
		}
	}
	public void LoadUser()
	{
		try
		{
			StreamReader reader = new StreamReader(filePath, Encoding.Default);
			using (reader)
			{
				string tag;
				//Debug.Log("LoadUser read from " + filePath);
				ccUtils.PositionAfter(reader, "User");
				string value = null;
				do
				{
					value = ccUtils.SDTNext(reader, out tag);
					if (value == null)
						continue;
					//Debug.Log("LoadUser got " + value + " for tag " + tag);
					switch (tag)
					{
						case "Name":
							this.user_name = value;
							//Debug.Log("LoadComponent adding to dict: " + this.user_name);
							user_dict.Add(this.user_name, this);
							break;
						case "PosIndex":
							if (!int.TryParse(value, out this.position))
							{
								Debug.Log("Error: LoadUser parsing position" + value);
							}
							break;
						case "Dept":
							this.department = value;
							break;
						case "InitialTraining":
							if (!int.TryParse(value, out this.training))
							{
								Debug.Log("Error: LoadUser parsing training" + value);
							}
							break;
					}
				}
				while (value != null);



			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message + "\n");
		}
	}
	public void SetFilePath(string path)
	{
		filePath = path;
	}
	void OnMouseDown()
	{
		Debug.Log("down for " + filePath + " user_name " + this.user_name);
		Debug.Log("thought: " + this.current_thought);
		foreach(string failed in this.failed_goals)
		{
			Debug.Log("failed goal: " + failed);
		}
		if (this.user_name == null || this.user_name.Length == 0)
		{
			Debug.Log("User name is empty");
			return;
		}
		//menus.clicked = "Component:" + this.component_name;

	}
	public void Configure()
	{
		Debug.Log("Configure");
		if (menus.clicked.EndsWith("Configure"))
		{
			menus.clicked = "";
			ConfigureCanvas();
		}


	}
	public void ConfigureCanvas()
	{
		//Debug.Log("ConfigureCanvas");

		GameObject user_panel = menus.menu_panels["UserPanel"];
		UserConfigure user_config_script = (UserConfigure)user_panel.GetComponent(typeof(UserConfigure));
		user_config_script.SetUser(this);
		user_panel.SetActive(true);
		menus.ActiveScreen(user_panel.name);
	}
	public static void doItems()
	{
		/* find the user that brought up a menu, and call its menuItems method */
		string user_name = menus.MenuLevel(1);
		//Debug.Log("look in dict for " + component_name);
		UserBehavior script = user_dict[user_name];
		int level = ccUtils.SubstringCount(menus.clicked, ":");
		if (level == 1)
		{
			WindowRect = GUI.Window(1, WindowRect, script.MenuItems, "Item");
		}
		else
		{
			string submenu = menus.MenuLevel(2);
			Debug.Log("submenu is <" + submenu + "> level is " + level + " clicked " + menus.clicked);
			switch (submenu)
			{
				case "Configure":
					Debug.Log("is configure");
					script.Configure();
					break;

			}

		}

	}
	private void MenuItems(int id)
	{
		if (GUILayout.Button("Help"))
		{
			menus.clicked = "help";
		}

		else if (GUILayout.Button("Configure"))
		{
			menus.clicked += ":Configure";
		}
		else if (GUILayout.Button("Close menu"))
		{
			menus.clicked = "";
		}
	}
	public void AddTraining(int add_amount)
	{
		// adjust training based on given amount, using old game engine algorithm
		// and advise engine of new training value.
		if (training <= 95)
		{
			int hack_cost = add_amount * 250;
			training = Math.Max(add_amount * 4, 5) + training;
			if (training > 95)
				training = 95;
			XElement xml = new XElement("userEvent",
				new XElement("train",
					new XElement("name", user_name),
					new XElement("level", training)),
				new XElement("cost", hack_cost));

			IPCManagerScript.SendRequest(xml.ToString());
		}
	}
	// Use this for initialization
	void Start () {
		
	}

}
