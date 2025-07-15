input("*.prefab")
{
    int("maxTriangleCount", 1000);
    bool("hasAnimation", false);
    stringlist("filter", "");
    stringlist("notfilter", "");
	float("pathwidth",240){range(20,4096);};
    feature("source", "sceneassets");
    feature("menu", "2.Current Scene Resources/Prefab");
    feature("description", "just so so");
}
filter
{
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
        $v0 = loadasset(assetpath);
        $v1 = collectprefabinfo($v0);
        order = $v1.triangleCount;
        if($v1.triangleCount >= maxTriangleCount && (!hasAnimation || $v1.clipCount>0)){
            info = format("skinned:{0},mesh:{1},vertex:{2},triangle:{3},bone:{4},material:{5},max_tex_size:({6},{7}),max_tex_name:{8}={9},clip:{10},max_keyframe:{11}",
                $v1.skinnedMeshCount, $v1.meshFilterCount, $v1.vertexCount, $v1.triangleCount, $v1.boneCount, $v1.materialCount, $v1.maxTexWidth, $v1.maxTexHeight, $v1.maxTexPropName, $v1.maxTexName, $v1.clipCount, $v1.maxKeyFrameCount
                );
            $r = 1;
        }else{
            $r = 0;
        };
        unloadasset($v0);
        $r;
    }else{
        0;
    };
};