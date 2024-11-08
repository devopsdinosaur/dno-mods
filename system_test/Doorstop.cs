using BepInEx;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace Doorstop {
    class Entrypoint {
        
        public static void Start() {
            
        }
    }
}

public class TestSystem : SystemBase {
    private const string LINKED_PLUGIN_NAME = "devopsdinosaur.dno.resource_fairy";
    private const string LINKED_CLASS_NAME = "ResourceFairyPlugin";

    private static StreamWriter m_log = null;
    private float m_elapsed = 0;
    private ResourceFairyInterface m_plugin = null;
    
    public static void _debug_log(object text) {
        if (m_log == null) {
            string this_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string log_path = Path.Combine(this_dir, "system_test.log");
            m_log = new StreamWriter(log_path);
        }
        m_log.WriteLine(text.ToString());
        m_log.Flush();
    }

    protected override void OnUpdate() {
        if ((this.m_elapsed += Time.DeltaTime) < 1) {
            return;
        }
        this.m_elapsed = 0;
        _debug_log(Time.ElapsedTime);
        if (this.m_plugin == null && (this.m_plugin = (ResourceFairyInterface) DDPlugin.Locator.locate("resource_fairy")) == null) {
            return;
        }
        _debug_log(".");
        this.m_plugin.testfunc("Hiya buddy!");
    }
}
