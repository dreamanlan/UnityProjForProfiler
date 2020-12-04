script(main)
{	
	$CacheServerPreferences = gettype("CacheServerPreferences");
	$LocalCacheServer = gettype("LocalCacheServer");
	$CacheServerPreferences.ReadPreferences();
	debugwarning("cache server {0}.", $CacheServerPreferences.s_CacheServerIPAddress);
	if($CacheServerPreferences.s_CacheServerIPAddress!="9.134.193.153:8126"){
		$CacheServerPreferences.s_CacheServerIPAddress="9.134.193.153:8126";
		$LocalCacheServer.Clear();
		debugerror("change cache server to 9.134.193.153:8126.");
		$CacheServerPreferences.WritePreferences();
	};
	
	$unityExe = cmdlineargs()[0];
	
	$Application = gettype("Application");
	$AssetDatabase = gettype("AssetDatabase");
	$EditorUtility = gettype("EditorUtility");
    $EditorSettings = gettype("EditorSettings");
	$UnityM1Version = gettype("UnityM1Version");
	$EditorPrefs = gettype("EditorPrefs");
	
	$EditorPrefs.SetInt("AndroidJVMMaxHeapSize",8192);
	
	$assetPath = $Application.dataPath;
	debugerror("assetpath:{0} unitypath:{1}",$assetPath,$unityExe);
		
    if(isnull($UnityM1Version)){
        $unitym1version = -1;       
        $md5 = calcmd5($unityExe);
        debugerror("unity need update ! cur version:{0} md5:{1} update svn:http://bj-scm.tencent.com/qsmy/qs_code_proj/trunk/Unity", $unitym1version, $md5);
        displaydialog("", format("你的unity3d编辑器必须更新到m1的最新版本才能继续工作！\n当前版本:{0} \nMD5:{1} \n当前安装位置:{2} \n请从svn更新新版本：http://bj-scm.tencent.com/qsmy/qs_code_proj/trunk/Unity", $unitym1version, $md5, cmdline()), "ok");
    	return(true);
    }elseif($UnityM1Version.Version<=16777217){
        $unitym1version = $UnityM1Version.Version;
        $md5 = calcmd5($unityExe);
        debugerror("unity need update ! cur version:{0} md5:{1} update svn:http://bj-scm.tencent.com/qsmy/qs_code_proj/trunk/Unity", $unitym1version, $md5);
        displaydialog("", format("你的unity3d编辑器必须更新到m1的最新版本才能继续工作！\n当前版本:{0} \nMD5:{1} \n当前安装位置:{2} \n请从svn更新新版本：http://bj-scm.tencent.com/qsmy/qs_code_proj/trunk/Unity", $unitym1version, $md5, cmdline()), "ok");
    	return(true);
    }else{
        if((isnull(@@haskey) || !@@haskey) && $EditorSettings.serializationMode!=parseenum("SerializationMode", "Mixed")){
            if(displaydialog("", format("如果你的编辑器反复re-serializing，请选择‘修复并退出’然后重新运行unity，否则请点击‘继续’", $unitym1version, $md5, cmdline()), "修复并退出", "继续")){
				$EditorSettings.serializationMode = parseenum("SerializationMode", "Mixed");
				deletedir("Library/ScriptAssemblies");
				copyfile("EditorSettings.asset","ProjectSettings/EditorSettings.asset");
				if(osplatform()=="Win32NT"){
					process($assetPath+"/../../../Product/Tools/BatchCommand.exe", $assetPath+"/../../../Product/Tools/Batch/rununity.dsl "+$unityExe){
						nowait(true);
					};
					wait(3000);
				};
				return(true);
            };
    	};
		
		$rs = verifycacheserver();
		debugwarning("verify cache server: {0} {1}", $rs[0], $rs[1]);
				
        return(false);
    };
};

script(check)
{
    $EditorSettings = gettype("EditorSettings");
    if($EditorSettings.serializationMode!=parseenum("SerializationMode", "ForceText")){
        $EditorSettings.serializationMode = parseenum("SerializationMode", "ForceText");
		};
};