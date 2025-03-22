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
    bool("findasset", false);
    float("pathwidth",240){range(20,4096);};
    feature("source", "snapshot");
    feature("menu", "6.Memory/slowly group memory and mheaps");
    feature("description", "just so so");
}
filter
{
    if(isnull(@hash)){
        @hash = hashtable();
    };
    if(memory.size >= maxSize && stringcontains(memory.className, classfilter) && stringcontains(memory.name, filter) && stringnotcontains(memory.className, classnotfilter) && stringnotcontains(memory.name, notfilter)){
        if(findasset){
        		assetpath = (findasset(memory.name, memory.className))[0];
        }else{
            assetpath = memory.name;
        };
        if(isnullorempty(assetpath)){
            assetpath = memory.name;
        };
        $v0 = findmanagedheaps(mheaps, memory.address);
        if(isnull($v0)){
            $v1 = 0;
            $v2 = 0;
        }else{
            $v1 = $v0.size;
            $v2 = $v0.vm_start;
        };
        if(heapAddr==0 || heapAddr==$v2){
        		info = format("mheap:{0:X}", $v2);
        		scenepath = info;
            order = $v2;
            value = memory.size;
            group = assetpath;
            $key = assetpath+"-"+info;
            $r = hashtableget(@hash, $key);
            if(isnull($r)){
                hashtableadd(@hash, $key, 1);
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
    if(!isnullorempty($paramInfo.Value)){
        $paramInfo.Value = loadmanagedheaps($paramInfo.Value);
        return(3600.0);
    }else{
        return(1.0);
    };
};