using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using CsLibrary;

[InitializeOnLoad]
public class EditorInit
{
    static EditorInit()
    {
        var p = System.Diagnostics.Process.GetCurrentProcess();
        string pkey = string.Format("pid:{0}_{1}", p.Id, p.StartTime.ToString("yyyy-MM-dd_HH-mm-ss-fff"));
        s_HasKey = EditorPrefs.HasKey(pkey);
        if (!s_HasKey) {
            EditorPrefs.SetBool(pkey, true);
        }
        Debug.LogWarningFormat("process key {0}", pkey);
        EditorApplication.quitting += () => {
            EditorPrefs.DeleteKey(pkey);
            Debug.LogWarningFormat("delete key {0}", pkey);
        };
        s_ProcessKey = pkey;

        PlayerSettings.Android.keystorePass = "qsmy123456qsmy";
        PlayerSettings.Android.keyaliasName = "qsmy.keystore";
        PlayerSettings.Android.keyaliasPass = "qsmy123456qsmy";
        GlobalVariables.Instance.IsEditor = true;
                                    
        LogSystem.OnOutput = (Log_Type type, string msg) => {
            if (type == Log_Type.LT_Error)
                Debug.LogError(msg);
            else if (type == Log_Type.LT_Warn)
                Debug.LogWarning(msg);
            else
                Debug.Log(msg);
        };

        if (ExecuteUpdate("editor_update_check.dsl")) {
            EditorApplication.Exit(0);
        }
        else {
            EditorApplication.update += Update;

            BuildPlayerWindow.RegisterGetBuildPlayerOptionsHandler(opt => {
                var newOpt = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(opt);
                newOpt.options &= (~BuildOptions.StrictMode);
                return newOpt;
            });
        }
    }
    private static bool ExecuteUpdate(string dslFile)
    {
        try {
            if (!File.Exists(dslFile)) {
                return false;
            }
            bool ret = false;
            string txt = File.ReadAllText(dslFile);
            Dsl.DslFile file = new Dsl.DslFile();
            if (file.Load(dslFile, (string msg) => { Debug.LogError(msg); })) {
                s_Calculator = new DslExpression.DslCalculator();
                s_Calculator.Init();
                s_Calculator.SetGlobalVariable("@@processkey", s_ProcessKey);
                s_Calculator.SetGlobalVariable("@@haskey", s_HasKey);
                s_Calculator.SetGlobalVariable("global", DslExpression.CalculatorValue.FromObject(GlobalVariables.Instance));
                s_Calculator.Register("verifycacheserver", new DslExpression.ExpressionFactoryHelper<VerifyCacheServerExp>());

                foreach (var info in file.DslInfos) {
                    s_Calculator.LoadDsl(info);
                }
                object retObj = s_Calculator.Calc("main");
                if (null != retObj) {
                    ret = (bool)System.Convert.ChangeType(retObj, typeof(bool));
                }
            }
            return ret;
        }
        catch {
            return false;
        }
    }
    private static void ExecuteCheck()
    {
        try {
            if (null != s_Calculator) {
                s_Calculator.Calc("check");
            }
        }
        catch(System.Exception ex) {
            Debug.LogException(ex);
        }
    }

    private static void Update()
    {
        double curTime = EditorApplication.timeSinceStartup;
        if (s_Checked) {
            if (s_LastCheckTime + c_CheckInterval < curTime) {
                s_LastCheckTime = curTime;
                ExecuteCheck();
            }
        } else {
            if (null != EditorWindow.focusedWindow) {
                s_Checked = true;
                s_LastCheckTime = curTime;
                ExecuteCheck();
            }
        }
    }

    private static bool s_HasKey = false;
    private static string s_ProcessKey = string.Empty;
    private static DslExpression.DslCalculator s_Calculator = null;

    private static bool s_Checked = false;
    private static double s_LastCheckTime = 0;
    private const double c_CheckInterval = 30.0;
}

internal class VerifyCacheServerExp : DslExpression.SimpleExpressionBase
{
    protected override DslExpression.CalculatorValue OnCalc(IList<DslExpression.CalculatorValue> operands)
    {
        ulong r1 = UnityEditorInternal.InternalEditorUtility.VerifyCacheServerIntegrity();
        ulong r2 = 0;
        if (0 != r1) {
            r2 = UnityEditorInternal.InternalEditorUtility.FixCacheServerIntegrityErrors();
        }
        return DslExpression.CalculatorValue.FromObject(new ulong[] { r1, r2 });
    }
}
