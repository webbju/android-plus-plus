<?php

function t2t_fix_shortcodes($content){   
    $array = array (
        '<p>[' => '[', 
        ']</p>' => ']', 
        ']<br />' => ']'
    );

    $content = strtr($content, $array);
    return $content;
}
add_filter('the_content', 't2t_fix_shortcodes');

/*-----------------------------------------------------------------------------------*/
/*	Column Shortcodes
/*-----------------------------------------------------------------------------------*/
function t2t_one_third( $atts, $content = null ) {
   return '<div class="one_third">' . do_shortcode($content) . '</div>';
}

add_shortcode('one_third', 't2t_one_third');

function t2t_one_third_last( $atts, $content = null ) {
   return '<div class="one_third column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}

add_shortcode('one_third_last', 't2t_one_third_last');

function t2t_two_third( $atts, $content = null ) {
   return '<div class="two_third">' . do_shortcode($content) . '</div>';
}

add_shortcode('two_third', 't2t_two_third');

function t2t_two_third_last( $atts, $content = null ) {
   return '<div class="two_third column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}

add_shortcode('two_third_last', 't2t_two_third_last');

function t2t_one_half( $atts, $content = null ) {
   return '<div class="one_half">' . do_shortcode($content) . '</div>';
}

add_shortcode('one_half', 't2t_one_half');

function t2t_one_half_last( $atts, $content = null ) {
   return '<div class="one_half column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}

add_shortcode('one_half_last', 't2t_one_half_last');

function t2t_one_fourth( $atts, $content = null ) {
   return '<div class="one_fourth">' . do_shortcode($content) . '</div>';
}

add_shortcode('one_fourth', 't2t_one_fourth');

function t2t_one_fourth_last( $atts, $content = null ) {
   return '<div class="one_fourth column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}

add_shortcode('one_fourth_last', 't2t_one_fourth_last');

function t2t_three_fourth( $atts, $content = null ) {
   return '<div class="three_fourth">' . do_shortcode($content) . '</div>';
}

add_shortcode('three_fourth', 't2t_three_fourth');

function t2t_three_fourth_last( $atts, $content = null ) {
   return '<div class="three_fourth column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}

add_shortcode('three_fourth_last', 't2t_three_fourth_last');

function t2t_one_fifth( $atts, $content = null ) {
   return '<div class="one_fifth">' . do_shortcode($content) . '</div>';
}

add_shortcode('one_fifth', 't2t_one_fifth');

function t2t_one_fifth_last( $atts, $content = null ) {
   return '<div class="one_fifth column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}

add_shortcode('one_fifth_last', 't2t_one_fifth_last');

function t2t_two_fifth( $atts, $content = null ) {
   return '<div class="two_fifth">' . do_shortcode($content) . '</div>';
}

add_shortcode('two_fifth', 't2t_two_fifth');

function t2t_two_fifth_last( $atts, $content = null ) {
   return '<div class="two_fifth column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}
add_shortcode('two_fifth_last', 't2t_two_fifth_last');

function t2t_three_fifth( $atts, $content = null ) {
   return '<div class="three_fifth">' . do_shortcode($content) . '</div>';
}

add_shortcode('three_fifth', 't2t_three_fifth');

function t2t_three_fifth_last( $atts, $content = null ) {
   return '<div class="three_fifth column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}

add_shortcode('three_fifth_last', 't2t_three_fifth_last');

function t2t_four_fifth( $atts, $content = null ) {
   return '<div class="four_fifth">' . do_shortcode($content) . '</div>';
}

add_shortcode('four_fifth', 't2t_four_fifth');

function t2t_four_fifth_last( $atts, $content = null ) {
   return '<div class="four_fifth column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}

add_shortcode('four_fifth_last', 't2t_four_fifth_last');

function t2t_one_sixth( $atts, $content = null ) {
   return '<div class="one_sixth">' . do_shortcode($content) . '</div>';
}

add_shortcode('one_sixth', 't2t_one_sixth');

function t2t_one_sixth_last( $atts, $content = null ) {
   return '<div class="one_sixth column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}

add_shortcode('one_sixth_last', 't2t_one_sixth_last');

function t2t_five_sixth( $atts, $content = null ) {
   return '<div class="five_sixth">' . do_shortcode($content) . '</div>';
}

add_shortcode('five_sixth', 't2t_five_sixth');

function t2t_five_sixth_last( $atts, $content = null ) {
   return '<div class="five_sixth column_last">' . do_shortcode($content) . '</div><div class="clear"></div>';
}

add_shortcode('five_sixth_last', 't2t_five_sixth_last');


/*-----------------------------------------------------------------------------------*/
/*	Buttons
/*-----------------------------------------------------------------------------------*/
function t2t_button( $atts, $content = null ) {
	
	extract(shortcode_atts(array(
		'url'     	 => '#',
		'target'     => '_self',
		'style'   => 'white',
		'size'	=> 'small'
    ), $atts));
	
   return '<a class="button '.$size.' '.$style.'" href="'.$url.'" target="'.$target.'">' . do_shortcode($content) . '</a> ';
}

add_shortcode('button', 't2t_button');

function t2t_app_store_button( $atts, $content = null ) {
	
	extract(shortcode_atts(array(
		'url'     	  => '#',
		'target'      => '_self',
		'type'  		  => 'apple',
		'small_text'	=> 'Download now for',
		'large_text'  => 'iPhone',
		'class'       => '',
		'title'       => '',
		'rel'         => ''
    ), $atts));
	
	$out = "";				
	$out .= '<a href="'.$url.'" class="large_button" id="'.$type.'" rel="'.$rel.'" class="'.$class.'" title="'.$title.'">';
	$out .= '<span class="icon"></span>';
	$out .= '<small>'.$small_text.'</small>'.$large_text;
	$out .= '</a> ';
	
	return $out;
}
add_shortcode('app_store_button', 't2t_app_store_button');

/*-----------------------------------------------------------------------------------*/
/*	Tooltips
/*-----------------------------------------------------------------------------------*/

function t2t_tooltip( $atts, $content = null ) {
	
	extract(shortcode_atts(array(
		'url'     	 => '#',
		'target'     => '_self',
		'title'   => 'Tooltip Text'
    ), $atts));
	
   return '<a href="'.$url.'" rel="tipsy" title="'.$title.'" target="'.$target.'">'.do_shortcode($content).'</a>';
}

add_shortcode('tooltip', 't2t_tooltip');

/*-----------------------------------------------------------------------------------*/
/*	Toggle Shortcodes
/*-----------------------------------------------------------------------------------*/
function t2t_toggles( $atts, $content = null ) {
    
    $output = '<div class="toggle_list">';
		$output .= '<ul>';
        
    $myContent = do_shortcode($content);
    $output .= $myContent;
    $output .= '</ul>';
    $output .= '</div>';
    
    return $output;
}

add_shortcode('toggles', 't2t_toggles');

function t2t_toggle( $atts, $content = null ) {
    extract(shortcode_atts(array(
        'title' => 'notabletitle',
				'opened' => "false"
    ), $atts));
    
    if( $title == 'notabtitle' ) { return; }
		if( $opened == "true" ) {
			$toggle_class = "opened";
		} else {
			$toggle_class = "";
		}
    
    $output = '<li class="'. $toggle_class .'">';
		$output .= '<div class="title"><a href="javascript:;">'. do_shortcode($title) .'</a> <a href="javascript:;" class="toggle_link" data-open_text="+" data-close_text="-"></a></div>';
		$output .= '<div class="content">'. do_shortcode($content) . '</div>';
		$output .= '</li>';
		
    
    return $output;
}

add_shortcode('toggle', 't2t_toggle');

/*-----------------------------------------------------------------------------------*/
/*	Tabs Shortcodes
/*-----------------------------------------------------------------------------------*/
function t2t_tabs( $atts, $content = null ) {
    extract(shortcode_atts(array(
        'tabs' => 'notabtitles'
    ), $atts));
    
    if( $tabs == 'notabtitles' ) { return; }
    
    $output = '';
    $output .= '<div class="tabs">';
		$output .= '<ul class="nav">';
    
    $myTabs = explode(',', $tabs);
    foreach($myTabs as $tab) {
        $nospacetab = strtolower(str_replace(' ', '_', trim($tab)));
        $output .= '<li><a href="javascript:;" class="'. $nospacetab .'">' . $tab . '</a></li>';
    }
    
    $output .= '</ul>';
    $myContent = do_shortcode($content);
    $output .= $myContent;
    $output .= '</div>';
    
    return $output;
}

add_shortcode('tabs', 't2t_tabs');

function t2t_tabs_panes( $atts, $content = null ) {
    extract(shortcode_atts(array(
        'title' => 'notabletitle'
    ), $atts));
    
    if( $title == 'notabtitle' ) { return; }
    
    $nospacetab = trim(strtolower(str_replace(' ', '_', $title)));
    $output = '<div id="' . $nospacetab . '" class="pane">' . do_shortcode($content) . '</div>';
    
    return $output;
}

add_shortcode('tab', 't2t_tabs_panes');

/*-----------------------------------------------------------------------------------*/
/* Update list
/*-----------------------------------------------------------------------------------*/

function t2t_update_list( $atts, $content = null ) {
	return "<ul class='update_list'>".do_shortcode($content)."</ul>";
}

add_shortcode('update_list', 't2t_update_list');

function t2t_update_item( $atts, $content = null ) {
	
	extract(shortcode_atts(array(
      'color' => 'blue',
			'title'  => 'New'
  ), $atts));
	
	return '<li class="'.$color.'"><span><b>'.$title.'</b></span> '. do_shortcode($content) .'</li>';
	
}

add_shortcode('update_item', 't2t_update_item');

/*-----------------------------------------------------------------------------------*/
/* Team Members
/*-----------------------------------------------------------------------------------*/

function t2t_team( $atts, $content = null ) {
	extract(shortcode_atts(array(
      'layout' => 'two_column'
  ), $atts));

	return '<div class="team_members '.$layout.'">'.do_shortcode($content).'</div>';
}

add_shortcode('team', 't2t_team');

function t2t_team_member( $atts, $content = null ) {
	
	extract(shortcode_atts(array(
      'photo' => '',
			'name'  => '',
			'title' => '',
			'url_title' => '',
			'url' => ''
  ), $atts));
	
	$out = '';
	
	$out .= '<div class="person">';
	$out .= '<img src="'.$photo.'" alt="" />';
	$out .= '<h3>'.$name.'</h3>';
	$out .= '<span class="title">'.$title.'</span>';
	$out .= '<a href="'.$url.'" class="website">'.$url_title.'</a>';
	$out .= do_shortcode($content);
	$out .= '</div>';
	
	return $out;
	
}

add_shortcode('team_member', 't2t_team_member');

/*-----------------------------------------------------------------------------------*/
/* Social Links
/*-----------------------------------------------------------------------------------*/

function t2t_social( $atts, $content = null ) {
	return "<ul class='social'>".do_shortcode($content)."</ul>";
}

add_shortcode('social_links', 't2t_social');


function t2t_social_link( $atts, $content = null ) {
	
	extract(shortcode_atts(array(
      'site' => '',
			'target' => ''
  ), $atts));
	
	$out = '';
	
	if($site == "twitter") 				{ $social_links['twitter'] 			= array('&#62218;', $content); }
	if($site == "facebook") 			{ $social_links['facebook']     = array('&#62221;', $content); }
	if($site == "flickr") 				{ $social_links['flickr'] 	  	= array('&#62212;', $content); }
	if($site == "github") 				{ $social_links['github'] 	  	= array('&#62209;', $content); }
	if($site == "vimeo") 					{ $social_links['vimeo'] 	  	  = array('&#62215;', $content); }
	if($site == "google_plus") 		{ $social_links['google_plus'] 	= array('&#62224;', $content); }
	if($site == "pinterest") 			{ $social_links['pinterest'] 		= array('&#62227;', $content); }
	if($site == "linkedin") 			{ $social_links['linkedin'] 		= array('&#62233;', $content); }
	if($site == "dribbble") 			{ $social_links['dribbble'] 		= array('&#62236;', $content); }
	if($site == "stumbleupon") 		{ $social_links['stumbleupon'] 	= array('&#62239;', $content); }
	if($site == "lastfm") 				{ $social_links['lastfm'] 			= array('&#62242;', $content); }
	if($site == "rdio") 					{ $social_links['rdio'] 				= array('&#62245;', $content); }
	if($site == "spotify") 				{ $social_links['spotify'] 			= array('&#62248;', $content); }
	if($site == "skype") 					{ $social_links['skype'] 				= array('&#62266;', $content); }
	
	foreach ($social_links as $site => $url) {
		$out .= '<li><a href="'.$url[1].'" title="'.$site.'"><span class="social_icon">'.$url[0].'</span></a></li>';
	}
	
	return $out;
	
}

add_shortcode('social_link', 't2t_social_link');

?>