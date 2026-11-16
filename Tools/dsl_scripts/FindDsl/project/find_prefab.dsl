input("*.prefab")
{
    int("maxTriangleCount", 1000);
    int("subMeshCount", 1);
    bool("hasAnimation", false);
    bool("hasOffscreenUpdate", false);
    bool("hasAlwaysAnimate", false);
    stringlist("anyfilter", "");
    stringlist("notfilter", "");
    stringhash("hashkeys", "");
    stringhash("meshhashkeys", "");
    float("pathwidth",240){range(20,4096);};
    feature("source", "project");
    feature("menu", "1.Project Resources/Prefab");
    feature("description", "just so so");
}
filter
{
    $name=get_file_name_without_extension(asset_path);
    if(string_contains_any(asset_path, anyfilter) && string_not_contains(asset_path, notfilter) && string_hash_contains(hashkeys, $name)){
        $v0 = load_asset(asset_path);
        $v1 = collect_prefab_info($v0);
        $v2 = getcomponentinchildren($v0,"Playables.PlayableDirector").gameObject.name;
        if($v1.triangleCount >= maxTriangleCount && (!hasAnimation || $v1.clipCount>0) && (!hasOffscreenUpdate || $v1.updateWhenOffscreenCount>0) && (!hasAlwaysAnimate || $v1.alwaysAnimateCount>0)){
            $ret = 0;
            $key = "";
            loop_list($v1.meshes){
                $meshInfo = $$;
                $v3 = get_file_name($meshInfo.meshName);
                $subMeshCount = $meshInfo.subMeshCount;
                if(string_hash_contains(meshhashkeys, $v3) && $subMeshCount>=subMeshCount){
                    $ret = 1;
                    $key = $$.meshName;
                    break;
                };
            };
            if($ret){
                scene_path = $name;
                order = $v1.triangleCount;
                value = order;
                info = format("key:{0},timeline:{1},skinned:{2},mesh:{3},vertex:{4},triangle:{5},bone:{6},material:{7},max_tex_size:({8},{9}),max_tex_name:{10}={11},clip:{12},max_keyframe:{13}",
                        $key, $v2, $v1.skinnedMeshCount, $v1.meshFilterCount, $v1.vertexCount, $v1.triangleCount, $v1.boneCount, $v1.materialCount, $v1.maxTexWidth, $v1.maxTexHeight, $v1.maxTexPropName, $v1.maxTexName, $v1.clipCount, $v1.maxKeyFrameCount
                    );
            };
        }else{
            $ret = 0;
        };
        unload_asset($v0);
        $ret;
    }else{
        0;
    };
};