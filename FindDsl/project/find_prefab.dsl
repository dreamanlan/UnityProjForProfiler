input("*.prefab")
{
    int("maxTriangleCount", 1000);
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
    $name=getfilenamewithoutextension(assetpath);
    if(stringcontainsany(assetpath, anyfilter) && stringnotcontains(assetpath, notfilter) && stringhashcontains(hashkeys, $name)){
        $v0 = loadasset(assetpath);
        $v1 = collectprefabinfo($v0);
        $v2 = getcomponentinchildren($v0,"Playables.PlayableDirector").gameObject.name;
        //unloadasset($v0);
        if($v1.triangleCount >= maxTriangleCount && (!hasAnimation || $v1.clipCount>0) && (!hasOffscreenUpdate || $v1.updateWhenOffscreenCount>0) && (!hasAlwaysAnimate || $v1.alwaysAnimateCount>0)){
            $ret = 0;
            $key = "";
            looplist($v1.meshes){
                $v3 = getfilename($$.meshName);
                if(stringhashcontains(meshhashkeys, $v3)){
                    $ret = 1;
                    $key = $$.meshName;
                    break;
                };
            };
            if($ret){
                scenepath = $name;
                order = $v1.triangleCount;
                value = order;
                info = format("key:{0},timeline:{1},skinned:{2},mesh:{3},vertex:{4},triangle:{5},bone:{6},material:{7},max_tex_size:({8},{9}),max_tex_name:{10}={11},clip:{12},max_keyframe:{13}",
                        $key, $v2, $v1.skinnedMeshCount, $v1.meshFilterCount, $v1.vertexCount, $v1.triangleCount, $v1.boneCount, $v1.materialCount, $v1.maxTexWidth, $v1.maxTexHeight, $v1.maxTexPropName, $v1.maxTexName, $v1.clipCount, $v1.maxKeyFrameCount
                    );
            };
            $ret;
        }else{
            0;
        };
    }else{
        0;
    };
};