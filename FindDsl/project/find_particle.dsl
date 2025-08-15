input("*.prefab")
{
    int("maxSize",10);
    stringlist("anyfilter", "");
    stringlist("psanyfilter", "");
    stringhash("hashkeys", "");
    stringhash("pshashkeys", "");
    float("pathwidth",240){range(20,4096);};
    feature("source", "project");
    feature("menu", "1.Project Resources/Particle Systems");
    feature("description", "just so so");
}
filter
{
    object = loadasset(assetpath);
    $v0 = getcomponentsinchildren(object,"ParticleSystem");
    $v1 = $v0.Length;
    $name = getfilenamewithoutextension(assetpath);
    if($v1>maxSize && stringcontainsany(assetpath, anyfilter) && stringhashcontains(hashkeys, $name)){
        $ret = 0;
        $key = "";
        looplist($v0){
            $v2 = $$.name;
            if(stringcontainsany($v2, psanyfilter) && stringhashcontains(pshashkeys, $v2)){
                $ret = 1;
                $key = $v2;
                break;
            };
        };
        if($ret){
            $v3 = collectprefabinfo(object);
            $v4 = getruntimememory(object);
            $totalTriangleCount = $v3.triangleCount;
            scenepath = getfilenamewithoutextension(assetpath);
            info = format("key:{0} particle_count:{1} total_prefab_triangle:{2} memory:{3}",$key,$v1,$totalTriangleCount,$v4);
            order = $totalTriangleCount;
            value = $totalTriangleCount;
        };
        $ret;
    }else{
        0;
    };
};