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
    object = load_asset(asset_path);
    $v0 = getcomponentsinchildren(object,"ParticleSystem");
    $v1 = $v0.Length;
    $name = get_file_name_without_extension(asset_path);
    if($v1>maxSize && string_contains_any(asset_path, anyfilter) && string_hash_contains(hashkeys, $name)){
        $ret = 0;
        $key = "";
        loop_list($v0){
            $v2 = $$.name;
            if(string_contains_any($v2, psanyfilter) && string_hash_contains(pshashkeys, $v2)){
                $ret = 1;
                $key = $v2;
                break;
            };
        };
        if($ret){
            $v3 = collect_prefab_info(object);
            $v4 = get_runtime_memory_size(object);
            $totalTriangleCount = $v3.triangleCount;
            scene_path = get_file_name_without_extension(asset_path);
            info = format("key:{0} particle_count:{1} total_prefab_triangle:{2} memory:{3}",$key,$v1,$totalTriangleCount,$v4);
            order = $totalTriangleCount;
            value = $totalTriangleCount;
        };
        $ret;
    }else{
        0;
    };
};