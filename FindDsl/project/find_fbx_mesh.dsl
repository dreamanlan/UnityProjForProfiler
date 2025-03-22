input("*.fbx")
{
    int("triangleCount", 1000);
    int("componentCount", 3);
    stringlist("filter", "");
    stringlist("notfilter", "");
    stringlist("uvfilter", "");
	float("pathwidth",240){range(20,4096);};
    feature("source", "project");
    feature("menu", "1.Project Resources/Check Fbx Mesh");
    feature("description", "just so so");
}
filter
{
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter)){
        $v0 = loadasset(assetpath);
        $v1 = collectmeshinfo($v0, importer);
        //unloadasset($v0);
        order = $v1.triangleCount;
        if($v1.triangleCount >= triangleCount){
            $v2 = calcmeshvertexcomponentcount($v0,true);
            looplist($v2){
                $key = $$.Key;
                $val = $$.Value;
                if($val >= componentCount && stringcontains($key, uvfilter)){
                    $v3 = newitem();
                    $v3.AssetPath = assetpath;
                    $v3.Info = format("skinned:{0},mesh:{1},vertex:{2},triangle:{3},vertex components:{4} {5}",
                        $v1.skinnedMeshCount, $v1.meshFilterCount, $v1.vertexCount, $v1.triangleCount, $val, $key
                        );
                    $v3.Order = $val;
                    $v3.Value = 0;
                };
            };
        };
    };
    0;
};