input
{
    string("category", "native"){
        popup(["all","native","managed"])
    };
    int("maxSize", 8);
    ulong("sectionAddr",0);
	stringlist("classfilter", "");
	stringlist("filter", "");
	stringlist("classnotfilter", "");
	stringlist("notfilter", "");
	string("mheaps",""){
	    file("csv");
	    script("LoadManagedHeaps");
	};
	string("maps",""){
	    file("txt");
	    script("LoadMaps");
	};
	bool("find_asset", false);
	float("pathwidth",240){range(20,4096);};
	feature("source", "snapshot");
	feature("menu", "6.Memory/slowly find memory and smaps");
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
		$v0 = find_smaps(maps, memory.address);
		$v1 = find_managed_heaps(mheaps, memory.address);
		if(is_null($v0)){
		    $v2 = "unknown";
		    $v3 = 0;
		    $v4 = 0;
		    $v5 = 0;
		    $v6 = 0;
		}else{
		    $v2 = $v0.module;
		    $v3 = $v0.size;
		    $v4 = $v0.vm_start;
		    $v5 = $v0.rss;
		    $v6 = $v0.pss;
		};
		if(is_null($v1)){
		    $v7 = 0;
		    $v8 = 0;
		}else{
		    $v7 = $v1.size;
		    $v8 = $v1.vm_start;
		};
		if(sectionAddr==0 || sectionAddr==$v4){
    		scene_path = format("name:{0} class:{1} size:{2} addr:{3:X}",
    	        memory.name, memory.className, memory.size, memory.address
    	        );
    		info = format("module:{0} section_size:{1} section_start:{2:X} rss:{3} pss:{4} mheap_size:{5} mheap:{6:X}",
    	        $v2, $v3, $v4, $v5, $v6, $v7, $v8
    	        );
    	    order = $v4;
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
script(LoadMaps)args($paramInfo)
{
    if(!is_null_or_empty($paramInfo.Value)){
        $paramInfo.Value = load_smaps($paramInfo.Value);
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