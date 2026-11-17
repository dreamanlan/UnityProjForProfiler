input("dummy")
{   
    string("addrs", ""){
        file("txt");
    };
    string("maps1", ""){
        file("txt");
    };
    string("maps2", ""){
        file("txt");
    };
	float("pathwidth",240){range(20,4096);};
    feature("source", "list");
    feature("menu", "9.Table/compare alloc and maps");
    feature("description", "just so so");
}
filter
{
    $maps1 = load_maps(maps1);
    $maps2 = load_maps(maps2);
    $results = match_maps($maps1,$maps2);
    $diffs = calc_matched_maps_diff($results,1);
    $addrs = load_addrs(addrs);
    $ct = $addrs.Count;
    loop_list($addrs){
        $addr = $$;
        $r = find_maps($diffs,$addr);
        if(!is_null($r)){
            $item = new_item();
            $item.AssetPath = format("{0:X}",$addr);
            $item.ScenePath = $addr;
            $item.Info = $r.vm_start+"-"+$r.vm_end+" "+$r.size+" "+$r.module;
            $item.Order = $addr;
            $item.Value = $r.size;
        };
    };
    0;
};