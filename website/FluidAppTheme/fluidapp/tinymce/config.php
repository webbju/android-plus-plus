<?php

// App Store Buttons shortcode config
$tz_shortcodes['app_store_button'] = array(
	'params' => array(
		'url' => array(
			'std' => '',
			'type' => 'text',
			'label' => __('Button URL', 'textdomain'),
			'desc' => __('Add the button\'s url eg http://example.com', 'textdomain')
		),
		'type' => array(
			'type' => 'select',
			'label' => __('Button\'s Type', 'textdomain'),
			'desc' => __('Select the button\'s type, ie apple, android', 'textdomain'),
			'options' => array(
				'apple' => 'Apple',
				'android' => 'Android',
				'blackberry' => 'Blackberry',
				'windows' => 'Windows'
			)
		),
		'small_text' => array(
			'std' => 'Download now for',
			'type' => 'text',
			'label' => __('Small Text', 'textdomain'),
			'desc' => __('Specify the small text of the button', 'textdomain')
		),
		'large_text' => array(
			'std' => 'iPhone',
			'type' => 'text',
			'label' => __('Large Text', 'textdomain'),
			'desc' => __('Specify the large text of the button', 'textdomain'),
		)
	),
	'shortcode' => '[app_store_button url="{{url}}" small_text="{{small_text}}" large_text="{{large_text}}" type="{{type}}"]',
	'popup_title' => __('Insert App Store Button Shortcode', 'textdomain')
);

// Buttons shortcode config
$tz_shortcodes['button'] = array(
	'params' => array(
		'url' => array(
			'std' => '',
			'type' => 'text',
			'label' => __('Button URL', 'textdomain'),
			'desc' => __('Add the button\'s url eg http://example.com', 'textdomain')
		),
		'style' => array(
			'type' => 'select',
			'label' => __('Button\'s Style', 'textdomain'),
			'desc' => __('Select the button\'s style, ie the buttons color', 'textdomain'),
			'options' => array(
				'white' => 'White',
				'black' => 'Black',
				'green' => 'Green',
				'blue' => 'Blue',
				'teal' => 'Teal',
				'purple' => 'Purple',
				'red' => 'Red',
				'orange' => 'Orange',
				'grey' => 'Grey',
				'blue' => 'Blue'
			)
		),
		'size' => array(
			'type' => 'select',
			'label' => __('Button\'s Size', 'textdomain'),
			'desc' => __('Select the button\'s size', 'textdomain'),
			'options' => array(
				'small' => 'Small',
				'large' => 'Large'
			)
		),
		'content' => array(
			'std' => 'Button Text',
			'type' => 'text',
			'label' => __('Button\'s Text', 'textdomain'),
			'desc' => __('Add the button\'s text', 'textdomain'),
		)
	),
	'shortcode' => '[button url="{{url}}" style="{{style}}" size="{{size}}"] {{content}} [/button]',
	'popup_title' => __('Insert Button Shortcode', 'textdomain')
);

// Tabs
$tz_shortcodes['tabs'] = array(
    'params' => array(
        'tabs' => array(
            'type' => 'text',
            'label' => __('Tab Titles', 'eandc'),
            'desc' => __('Please enter the tab titles here, seperating each by a comma. They must match the tabs that are created below.', 'eandc')
        )
    ),
    'no_preview' => true,
    'shortcode' => '[tabs tabs="{{tabs}}"] {{child_shortcode}}  [/tabs]',
    'popup_title' => __('Insert Column Shortcode', 'eandc'),
    
    'child_shortcode' => array(
        'params' => array(
            'title' => array(
                'std' => 'Title',
                'type' => 'text',
                'label' => __('Tab Title', 'eandc'),
                'desc' => __('Title of the tab', 'eandc'),
            ),
            'content' => array(
                'std' => 'Tab Content',
                'type' => 'textarea',
                'label' => __('Tab Content', 'eandc'),
                'desc' => __('Add the tabs content', 'eandc')
            )
        ),
        'shortcode' => '[tab title="{{title}}"] {{content}} [/tab]',
        'clone_button' => __('Add Tab', 'eandc')
    )
);

// Toggle content shortcode config
$tz_shortcodes['toggle'] = array(
    'no_preview' => true,
    'shortcode' => '[toggles] {{child_shortcode}}  [/toggles]',
    'popup_title' => __('Insert Toggle Shortcode', 'eandc'),
    
    'child_shortcode' => array(
        'params' => array(
            'title' => array(
                'std' => 'Title',
                'type' => 'text',
                'label' => __('Toggle Title', 'eandc'),
                'desc' => __('Title of the toggle', 'eandc'),
            ),
            'content' => array(
                'std' => 'Toggle Content',
                'type' => 'textarea',
                'label' => __('Toggle Content', 'eandc'),
                'desc' => __('Add the toggle content', 'eandc')
            ),
						'opened' => array(
							'type' => 'select',
							'label' => __('Toggle Opened?', 'textdomain'),
							'desc' => __('Open this toggle by default', 'textdomain'),
							'options' => array(
								'false' => 'false',
								'true' => 'true'
							)
						),
        ),
        'shortcode' => '[toggle title="{{title}}" opened="{{opened}}"] {{content}} [/toggle]',
        'clone_button' => __('Add Toggle', 'eandc')
    )
);

// Columns
$tz_shortcodes['columns'] = array(
	'params' => array(),
	'shortcode' => ' {{child_shortcode}} ', // as there is no wrapper shortcode
	'popup_title' => __('Insert Columns Shortcode', 'textdomain'),
	'no_preview' => true,
	
	// child shortcode is clonable & sortable
	'child_shortcode' => array(
		'params' => array(
			'column' => array(
				'type' => 'select',
				'label' => __('Column Type', 'textdomain'),
				'desc' => __('Select the type, ie width of the column.', 'textdomain'),
				'options' => array(
					'one_third' => 'One Third',
					'one_third_last' => 'One Third Last',
					'two_third' => 'Two Thirds',
					'two_third_last' => 'Two Thirds Last',
					'one_half' => 'One Half',
					'one_half_last' => 'One Half Last',
					'one_fourth' => 'One Fourth Last',
					'three_fourth' => 'Three Fourth',
					'three_fourth_last' => 'Three Fourth Last',
					'one_fifth' => 'One Fifth',
					'one_fifth_last' => 'One Fifth Last',
					'two_fifth' => 'Two Fifth',
					'two_fifth_last' => 'Two Fifth Last',
					'three_fifth' => 'Three Fifth',
					'three_fifth_last' => 'Three Fifth Last',
					'four_fifth' => 'Four Fifth',
					'four_fifth_last' => 'Four Fifth Last',
					'one_sixth' => 'One Sixth',
					'one_sixth_last' => 'One Sixth Last',
					'five_sixth' => 'Five Sixth',
					'five_sixth_last' => 'Five Sixth Last'
				)
			),
			'content' => array(
				'std' => '',
				'type' => 'textarea',
				'label' => __('Column Content', 'textdomain'),
				'desc' => __('Add the column content.', 'textdomain'),
			)
		),
		'shortcode' => '[{{column}}] {{content}} [/{{column}}] ',
		'clone_button' => __('Add Column', 'textdomain')
	)
);

?>