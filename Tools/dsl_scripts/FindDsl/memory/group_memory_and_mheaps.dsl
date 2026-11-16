input
{
    string("category", "native"){
        popup(["all","native","managed"])
    };
    string("style", "grouplist"){
        popup(["itemlist", "grouplist"]);
    };
    int("maxSize", 8);
    ulong("heapAddr", 0);
    stringlist("classfilter", "");
    stringlist("filter", "");
    stringlist("classnotfilter", "");
    stringlist("notfilter", "");
    string("mheaps",""){
        file("csv");
        script("LoadManagedHeaps");
    };
    bool("find_asset", false);
    float("pathwidth",240){range(20,4096);};
    feature("source", "snapshot");
    feature("menu", "6.Memory/slowly group memory and mheaps");
    feature("description", "just so so");
}
filter
{
    if(is_null(@hash)){
        @hash = hashtable();
    };
    if(memory.size >= maxSize && string_contains(memory.className, classfilter) && string_contains(memory.name, filter) && string_not_contains(memory.className, classnotfilter) && string_not_contains(memory.name, notfilter)){
        if(find_asset){
        		asset_path = (find_asset(memory.name, memory.className))[0];
        }else{
            asset_path = memory.name;
        };
        if(is_null_or_empty(asset_path)){
            asset_path = memory.name;
        };
        $v0 = find_managed_heaps(mheaps, memory.address);
        if(is_null($v0)){
            $v1 = 0;
            $v2 = 0;
        }else{
            $v1 = $v0.size;
            $v2 = $v0.vm_start;
        };
        if(heapAddr==0 || heapAddr==$v2){
        		info = format("mheap:{0:X}", $v2);
        		scene_path = info;
            order = $v2;
            value = memory.size;
            group = asset_path;
            $key = asset_path+"-"+info;
            $r = hashtable_get(@hash, $key);
            if(is_null($r)){
                hashtable_add(@hash, $key, 1);
                1;
            }else{
                0;
            };
        }else{
            0;
        };
    }else{
        0;
    };
}
group
{
		order = count;
		value = count;
		info = format("{0}=>{1}", group, count);
	  1;
};

script(LoadManagedHeaps)args($paramInfo)
{
    if(!is_null_or_empty($paramInfo.Value)){
        $paramInfo.Value = load_managed_heaps($paramInfo.Value);
        return(3600.0);
    }else{
        return(1.0);
    };
};