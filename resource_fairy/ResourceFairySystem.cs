using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

public class TestSystem : SystemBase {
    private ResourceFairyPlugin m_plugin;
    private float m_elapsed = 0;
    
    protected override void OnCreate() {
        this.m_plugin = new ResourceFairyPlugin();
        this.m_plugin.ecs_load();
    }

    protected override void OnUpdate() {
        if ((this.m_elapsed += Time.DeltaTime) < 1) {
            return;
        }
        this.m_elapsed = 0;
        DDPlugin._debug_log(Time.ElapsedTime);
    }
}
