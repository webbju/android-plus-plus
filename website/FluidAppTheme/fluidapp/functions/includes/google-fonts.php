<?php

if ( ! function_exists( 'get_google_fonts' ) )	:
	
function t2t_get_google_fonts() {
	$font_list = get_transient( 't2t_google_fonts_alpha' );	
	
	if ( false === $font_list )	:
	
		$url = "https://www.googleapis.com/webfonts/v1/webfonts?key=AIzaSyCjae0lAeI-4JLvCgxJExjurC4whgoOigA&sort=alpha";
	
		$request = wp_remote_get($url);
	
		if( is_wp_error( $request ) ) {
			
		   $error_message = $request->get_error_message();
		   echo "Something went wrong: $error_message";
		
		} else {
			
			$json = wp_remote_retrieve_body($request);
			
			$data = json_decode($json, TRUE);

			$items = $data['items'];
			$i = 0;
			foreach ($items as $item) {
			    $i++;

				$variants = array();
				foreach ($item['variants'] as $variant) {
			      $variants[] = $variant;
			    }

				$font_list[] = array('uid' => $i, 'family' => $item['family'], 'variants' => implode('|', $variants));

			}
			set_transient( 't2t_google_fonts_alpha', $font_list, 60 * 60 * 24 );
			
		}
	
	endif;
	
	return $font_list;
}

endif;
