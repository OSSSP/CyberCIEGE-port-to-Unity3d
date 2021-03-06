﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.Xml.Linq;
public class PhysicalSettings
{

	public Dictionary<string, bool> phys_dict = new Dictionary<string, bool>();
	public List<string> users_allowed = new List<string>();
	public List<string> groups_allowed = new List<string>();
	ZoneBehavior zone = null;
	public PhysicalSettings()
	{
		this.zone = zone;
		foreach (string key in GameLoadBehavior.physical_settings.proc_dict.Keys)
		{
			this.phys_dict[key] = false;
			//Debug.Log("LoadComputer proc key " + key);
		}
	}
	public bool HandleConfigurationSetting(string tag, string value)
	{
		//Debug.Log("handleConfig  " + tag + " " + value);
		bool retval = true;
		if (phys_dict.ContainsKey(tag))
		{
			bool result = false;
			if (!bool.TryParse(value, out result))
			{
				Debug.Log("Error PhysicalSettings parse " + tag);
			}
			phys_dict[tag] = result;
		} else if (tag == "PermittedUsers") {
			//Debug.Log("PhysicalSettings PermittedUsers " + value);
			if (UserBehavior.user_dict.ContainsKey(value))
			{
				users_allowed.Add(value);
				//Debug.Log("PhysicalSettings add user " + value);
			} else if (value.StartsWith("*.")){
				groups_allowed.Add(value.Substring(2));
				//Debug.Log("PhysicalSettings add group " + value);
			}
		}
		else
		{
			retval = false;
		}

		return retval;
	}
	public void ConfigureCanvas(ZoneBehavior zone, ZoneConfigure zone_config_script)
	{
		Debug.Log("PhysicalSettings ConfigureCanvas for " + zone.zone_name+ "items in dict: "+this.phys_dict.Count);
		zone_config_script.SetPhys(this.phys_dict, zone);
		Dictionary<string, bool> user_access_dict = new Dictionary<string, bool>();
		Dictionary<string, bool> group_access_dict = new Dictionary<string, bool>();
		foreach (string key in UserBehavior.user_dict.Keys)
		{
			bool allowed = false;
			if (this.users_allowed.Contains(key))
				allowed = true;
			user_access_dict[key] = allowed;
		}
		foreach (string key in DACGroups.group_dict.Keys)
		{
			bool allowed = false;
			if (this.groups_allowed.Contains(key))
				allowed = true;
			group_access_dict[key] = allowed;
		}

		zone_config_script.SetAccess(user_access_dict, zone);
		zone_config_script.SetAccess(group_access_dict, zone);
		this.zone = zone;
	}
	public void PhysChanged(Toggle toggle)
	{
		string field = toggle.GetComponentInChildren<Text>().text;
		Debug.Log("Zone PhysChanged " + field + " to " + toggle.isOn);
		this.phys_dict[field] = toggle.isOn;

		XElement xml = new XElement("zoneEvent",
			new XElement("name", this.zone.zone_name),
			new XElement("setting",
				new XElement("field", field + ":"),
				new XElement("value", toggle.isOn)));
		Debug.Log (xml.ToString ());
		IPCManagerScript.SendRequest(xml.ToString());
	}
	public void AccessChanged(Toggle toggle)
	{
		string field = toggle.GetComponentInChildren<Text>().text;
		Debug.Log("Zone AccessChanged " + field + " to " + toggle.isOn);
		string user_or_group = "user";
		string add_or_remove = "accessAdd";
		if (UserBehavior.user_dict.ContainsKey(field))
		{
			if (toggle.isOn)
			{
				this.users_allowed.Add(field);
			}
			else
			{
				add_or_remove = "accessRemove";
				this.users_allowed.Remove(field);
			}
		}
		else
		{
			user_or_group = "group";
			if (toggle.isOn)
			{
				this.groups_allowed.Add(field);
			}
			else
			{
				add_or_remove = "accessRemove";
				this.groups_allowed.Remove(field);
			}
		}

		XElement xml = new XElement("zoneEvent",
			new XElement("name", this.zone.zone_name),
			new XElement(add_or_remove,
				new XElement(user_or_group, field)));

		IPCManagerScript.SendRequest(xml.ToString());
	}
}
