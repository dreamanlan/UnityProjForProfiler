input("*.fbx")
{
    int("maxTriangleCount", 1000);
    bool("hasAnimation", false);
    bool("hasOffscreenUpdate", false);
    bool("hasAlwaysAnimate", false);
    bool("readable", false);
    stringlist("anyfilter", "");
    stringlist("notfilter", "");
    stringhash("hashkeys", "");
    stringhash("meshhashkeys", "");
    float("pathwidth",240){range(20,4096);};
    feature("source", "project");
    feature("menu", "1.Project Resources/Fbx");
    feature("description", "just so so");
}
filter
{
    $name = getfilenamewithoutextension(assetpath);
    if(stringcontainsany(assetpath, anyfilter) && stringnotcontains(assetpath, notfilter) && stringhashcontains(hashkeys, $name)){
        $v0 = loadasset(assetpath);
        $v1 = collectmeshinfo($v0, importer);
        $v2 = importer.isReadable;
        if($v1.triangleCount >= maxTriangleCount && (!hasAnimation || $v1.clipCount>0) && (!hasOffscreenUpdate || $v1.updateWhenOffscreenCount>0) && (!hasAlwaysAnimate || $v1.alwaysAnimateCount>0) && (!readable || $v2)){
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
                info = format("key:{0},skinned:{1},mesh:{2},vertex:{3},triangle:{4},bone:{5},material:{6},max_tex_size:({7},{8}),max_tex_name:{9}={10},clip:{11},max_keyframe:{12}",
                    $key, $v1.skinnedMeshCount, $v1.meshFilterCount, $v1.vertexCount, $v1.triangleCount, $v1.boneCount, $v1.materialCount, $v1.maxTexWidth, $v1.maxTexHeight, $v1.maxTexPropName, $v1.maxTexName, $v1.clipCount, $v1.maxKeyFrameCount
                );
            };
        }else{
            $ret = 0;
        };
        unloadasset($v0);
        $ret;
    }else{
        0;
    };
};