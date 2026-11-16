input("dummy")
{   
    string("maps1", ""){
        file("txt");
    };
    string("maps2", ""){
        file("txt");
    };
    string("encoding", "utf-8");
	int("resultopt",1){
		toggle(["all","onlyalone","onlynotalone"],[1,2,3]);
	};
	float("pathwidth",240){range(20,4096);};
    feature("source", "list");
    feature("menu", "9.Table/compare maps");
    feature("description", "just so so");
}
filter
{
    $maps1 = load_maps(maps1);
    $maps2 = load_maps(maps2);
    $results = match_maps($maps1,$maps2);
    loop_list($results){
        $row = $$;
        $left = $row[0];
        $right = $row[1];
        if(resultopt==1 || resultopt==2 && (is_null($left) || is_null($right)) || resultopt==3 && !is_null($left) && !is_null($right)){
            $leftInfo = "";
            $rightInfo = "";
            $val = 0;
            $module = "";
            $item = new_item();
            if(!is_null($left)){
                $leftInfo = format("{0:X}-{1:X}", $left.vm_start, $left.vm_end);
                if(resultopt==2){
                    $val = $val - $left.size;
                };
                $module=$left.module;
            };
            if(!is_null($right)){
                $rightInfo = format("{0:X}-{1:X}", $right.vm_start, $right.vm_end);            
                $val = $val + $right.size;
                $module=$right.module;
            };
            $item.AssetPath = $module;
            $item.ScenePath = $leftInfo+"<=>"+$rightInfo;
            $item.Info = $leftInfo + " <=> " + $rightInfo + " " + $val + " " + $module;
            $item.Order = $val;
            $item.Value = $val;
        };
    };
    0;
};