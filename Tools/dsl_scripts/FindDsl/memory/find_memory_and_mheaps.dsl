input
{
    string("category", "native"){
        popup(["all","native","managed"])
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
	feature("menu", "6.Memory/slowly find memory and mheaps");
	feature("description", "just so so");
}
filter
{
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
    		scene_path = format("name:{0} class:{1} size:{2} addr:{3:X}",
    	        memory.name, memory.className, memory.size, memory.address
    	        );
    		info = format("mheap_size:{0} mheap:{1:X}",
    	        $v1, $v2
    	        );
    	    order = $v2;
    	    value = memory.size;
        	extraobject = memory.objectData;
        	extralistbuild = "BuildExtraList";
    	    1;
    	}else{
    	    0;
    	};
	}else{
	    0;
	};
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
script(BuildExtraList)args($item)
{
	$r = find_shortest_path_to_root($item.ExtraObject);
	return($r);
};