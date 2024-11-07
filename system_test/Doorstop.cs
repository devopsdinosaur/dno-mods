using System;
using System.IO;
using System.Reflection;
using Unity.Entities;

namespace Doorstop {
    class Entrypoint {
        
        public static void Start() {
            //EcsPreloader.Instance.load_plugins();  
        }


        //AppDomain currentDomain = AppDomain.CurrentDomain;
        //currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromSameFolder);

        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args) {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
    }
}
/*
public class EcsPreloader {
    public const string OFFSET_TO_BEPINEX = "../BepInEx";

    private static EcsPreloader m_instance = null;
    public static EcsPreloader Instance {
        get {
            if (m_instance == null) {
                m_instance = new EcsPreloader();
            }
            return m_instance;
        }
    }
    private static StreamWriter m_log = null;
    private string m_bepinex_root;

    public static void _debug_log(object text) {
        if (m_log == null) {
            string this_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string log_path = Path.Combine(this_dir, "system_test.log");
            m_log = new StreamWriter(log_path);
        }
        m_log.WriteLine(text.ToString());
        m_log.Flush();
    }

    private EcsPreloader() {
        string this_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        this.m_bepinex_root = Path.GetFullPath(Path.Combine(this_dir, OFFSET_TO_BEPINEX));
    }

    public void load_plugins() {

    }
}
*/

public class TestSystem : SystemBase {
    private static StreamWriter m_log = null;
    private float m_elapsed = 0;
    
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
        if ((this.m_elapsed += Time.DeltaTime) < 1.0) {
            return;
        }
        this.m_elapsed = 0;
        _debug_log(Time.ElapsedTime);
    }
}
