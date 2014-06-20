/* 
 * More info at: http://phpjs.org
 * 
 * This is version: 3.24
 * php.js is copyright 2011 Kevin van Zonneveld.
 * 
 * Portions copyright Brett Zamir (http://brett-zamir.me), Kevin van Zonneveld
 * (http://kevin.vanzonneveld.net), Onno Marsman, Theriault, Michael White
 * (http://getsprink.com), Waldo Malqui Silva, Paulo Freitas, Jonas Raoni
 * Soares Silva (http://www.jsfromhell.com), Jack, Philip Peterson, Ates Goral
 * (http://magnetiq.com), Legaev Andrey, Ratheous, Alex, Martijn Wieringa,
 * Nate, lmeyrick (https://sourceforge.net/projects/bcmath-js/), Enrique
 * Gonzalez, Philippe Baumann, Rafał Kukawski (http://blog.kukawski.pl),
 * Webtoolkit.info (http://www.webtoolkit.info/), Ole Vrijenhoek, Ash Searle
 * (http://hexmen.com/blog/), travc, Carlos R. L. Rodrigues
 * (http://www.jsfromhell.com), Jani Hartikainen, stag019, GeekFG
 * (http://geekfg.blogspot.com), WebDevHobo (http://webdevhobo.blogspot.com/),
 * Erkekjetter, pilus, Rafał Kukawski (http://blog.kukawski.pl/), Johnny Mast
 * (http://www.phpvrouwen.nl), T.Wild,
 * http://stackoverflow.com/questions/57803/how-to-convert-decimal-to-hex-in-javascript,
 * d3x, Michael Grier, Andrea Giammarchi (http://webreflection.blogspot.com),
 * marrtins, Mailfaker (http://www.weedem.fr/), Steve Hilder, gettimeofday,
 * mdsjack (http://www.mdsjack.bo.it), felix, majak, Steven Levithan
 * (http://blog.stevenlevithan.com), Mirek Slugen, Oleg Eremeev, Felix
 * Geisendoerfer (http://www.debuggable.com/felix), Martin
 * (http://www.erlenwiese.de/), gorthaur, Lars Fischer, Joris, AJ, Paul Smith,
 * Tim de Koning (http://www.kingsquare.nl), KELAN, Josh Fraser
 * (http://onlineaspect.com/2007/06/08/auto-detect-a-time-zone-with-javascript/),
 * Chris, Marc Palau, Kevin van Zonneveld (http://kevin.vanzonneveld.net/),
 * Arpad Ray (mailto:arpad@php.net), Breaking Par Consulting Inc
 * (http://www.breakingpar.com/bkp/home.nsf/0/87256B280015193F87256CFB006C45F7),
 * Nathan, Karol Kowalski, David, Dreamer, Diplom@t (http://difane.com/), Caio
 * Ariede (http://caioariede.com), Robin, Imgen Tata (http://www.myipdf.com/),
 * Pellentesque Malesuada, saulius, Aman Gupta, Sakimori, Tyler Akins
 * (http://rumkin.com), Thunder.m, Public Domain
 * (http://www.json.org/json2.js), Michael White, Kankrelune
 * (http://www.webfaktory.info/), Alfonso Jimenez
 * (http://www.alfonsojimenez.com), Frank Forte, vlado houba, Marco, Billy,
 * David James, madipta, noname, sankai, class_exists, Jalal Berrami, ger,
 * Itsacon (http://www.itsacon.net/), Scott Cariss, nobbler, Arno, Denny
 * Wardhana, ReverseSyntax, Mateusz "loonquawl" Zalega, Slawomir Kaniecki,
 * Francois, Fox, mktime, Douglas Crockford (http://javascript.crockford.com),
 * john (http://www.jd-tech.net), Oskar Larsson Högfeldt
 * (http://oskar-lh.name/), marc andreu, Nick Kolosov (http://sammy.ru), date,
 * Marc Jansen, Steve Clay, Olivier Louvignes (http://mg-crea.com/), Soren
 * Hansen, merabi, Subhasis Deb, josh, T0bsn, Tim Wiel, Brad Touesnard, MeEtc
 * (http://yass.meetcweb.com), Peter-Paul Koch
 * (http://www.quirksmode.org/js/beat.html), Pyerre, Jon Hohle, duncan, Bayron
 * Guevara, Adam Wallner (http://web2.bitbaro.hu/), paulo kuong, Gilbert,
 * Lincoln Ramsay, Thiago Mata (http://thiagomata.blog.com), Linuxworld,
 * lmeyrick (https://sourceforge.net/projects/bcmath-js/this.), djmix, Bryan
 * Elliott, David Randall, Sanjoy Roy, jmweb, Francesco, Stoyan Kyosev
 * (http://www.svest.org/), J A R, kenneth, T. Wild, Ole Vrijenhoek
 * (http://www.nervous.nl/), Raphael (Ao RUDLER), Shingo, LH, JB, nord_ua, jd,
 * JT, Thomas Beaucourt (http://www.webapp.fr), Ozh, XoraX
 * (http://www.xorax.info), EdorFaus, Eugene Bulkin (http://doubleaw.com/),
 * Der Simon (http://innerdom.sourceforge.net/), 0m3r, echo is bad,
 * FremyCompany, stensi, Kristof Coomans (SCK-CEN Belgian Nucleair Research
 * Centre), Devan Penner-Woelk, Pierre-Luc Paour, Martin Pool, Brant Messenger
 * (http://www.brantmessenger.com/), Kirk Strobeck, Saulo Vallory, Christoph,
 * Wagner B. Soares, Artur Tchernychev, Valentina De Rosa, Jason Wong
 * (http://carrot.org/), Daniel Esteban, strftime, Rick Waldron, Mick@el,
 * Anton Ongson, Bjorn Roesbeke (http://www.bjornroesbeke.be/), Simon Willison
 * (http://simonwillison.net), Gabriel Paderni, Philipp Lenssen, Marco van
 * Oort, Bug?, Blues (http://tech.bluesmoon.info/), Tomasz Wesolowski, rezna,
 * Eric Nagel, Evertjan Garretsen, Luke Godfrey, Pul, Bobby Drake, uestla,
 * Alan C, Ulrich, Zahlii, Yves Sucaet, sowberry, Norman "zEh" Fuchs, hitwork,
 * johnrembo, Brian Tafoya (http://www.premasolutions.com/), Nick Callen,
 * Steven Levithan (stevenlevithan.com), ejsanders, Scott Baker, Philippe
 * Jausions (http://pear.php.net/user/jausions), Aidan Lister
 * (http://aidanlister.com/), Rob, e-mike, HKM, ChaosNo1, metjay, strcasecmp,
 * strcmp, Taras Bogach, jpfle, Alexander Ermolaev
 * (http://snippets.dzone.com/user/AlexanderErmolaev), DxGx, kilops, Orlando,
 * dptr1988, Le Torbi, James (http://www.james-bell.co.uk/), Pedro Tainha
 * (http://www.pedrotainha.com), James, penutbutterjelly, Arnout Kazemier
 * (http://www.3rd-Eden.com), 3D-GRAF, daniel airton wermann
 * (http://wermann.com.br), jakes, Yannoo, FGFEmperor, gabriel paderni, Atli
 * Þór, Maximusya, Diogo Resende, Rival, Howard Yeend, Allan Jensen
 * (http://www.winternet.no), davook, Benjamin Lupton, baris ozdil, Greg
 * Frazier, Manish, Matt Bradley, Cord, fearphage
 * (http://http/my.opera.com/fearphage/), Matteo, Victor, taith, Tim de
 * Koning, Ryan W Tenney (http://ryan.10e.us), Tod Gentille, Alexander M
 * Beedie, Riddler (http://www.frontierwebdev.com/), Luis Salazar
 * (http://www.freaky-media.com/), Rafał Kukawski, T.J. Leahy, Luke Smith
 * (http://lucassmith.name), Kheang Hok Chin (http://www.distantia.ca/),
 * Russell Walker (http://www.nbill.co.uk/), Jamie Beck
 * (http://www.terabit.ca/), Garagoth, Andrej Pavlovic, Dino, Le Torbi
 * (http://www.letorbi.de/), Ben (http://benblume.co.uk/), DtTvB
 * (http://dt.in.th/2008-09-16.string-length-in-bytes.html), Michael, Chris
 * McMacken, setcookie, YUI Library:
 * http://developer.yahoo.com/yui/docs/YAHOO.util.DateLocale.html, Andreas,
 * Blues at http://hacks.bluesmoon.info/strftime/strftime.js, rem, Josep Sanz
 * (http://www.ws3.es/), Cagri Ekin, Lorenzo Pisani, incidence, Amirouche, Jay
 * Klehr, Amir Habibi (http://www.residence-mixte.com/), Tony, booeyOH, meo,
 * William, Greenseed, Yen-Wei Liu, Ben Bryan, Leslie Hoare, mk.keck
 * 
 * Dual licensed under the MIT (MIT-LICENSE.txt)
 * and GPL (GPL-LICENSE.txt) licenses.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL KEVIN VAN ZONNEVELD BE LIABLE FOR ANY CLAIM, DAMAGES
 * OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */ 


function base64_decode (data) {
    // Decodes string using MIME base64 algorithm  
    // 
    // version: 1103.1210
    // discuss at: http://phpjs.org/functions/base64_decode
    // +   original by: Tyler Akins (http://rumkin.com)
    // +   improved by: Thunder.m
    // +      input by: Aman Gupta
    // +   improved by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
    // +   bugfixed by: Onno Marsman
    // +   bugfixed by: Pellentesque Malesuada
    // +   improved by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
    // +      input by: Brett Zamir (http://brett-zamir.me)
    // +   bugfixed by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
    // -    depends on: utf8_decode
    // *     example 1: base64_decode('S2V2aW4gdmFuIFpvbm5ldmVsZA==');
    // *     returns 1: 'Kevin van Zonneveld'
    // mozilla has this native
    // - but breaks in 2.0.0.12!
    //if (typeof this.window['btoa'] == 'function') {
    //    return btoa(data);
    //}
    var b64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
    var o1, o2, o3, h1, h2, h3, h4, bits, i = 0,
        ac = 0,
        dec = "",
        tmp_arr = [];

    if (!data) {
        return data;
    }

    data += '';

    do { // unpack four hexets into three octets using index points in b64
        h1 = b64.indexOf(data.charAt(i++));
        h2 = b64.indexOf(data.charAt(i++));
        h3 = b64.indexOf(data.charAt(i++));
        h4 = b64.indexOf(data.charAt(i++));

        bits = h1 << 18 | h2 << 12 | h3 << 6 | h4;

        o1 = bits >> 16 & 0xff;
        o2 = bits >> 8 & 0xff;
        o3 = bits & 0xff;

        if (h3 == 64) {
            tmp_arr[ac++] = String.fromCharCode(o1);
        } else if (h4 == 64) {
            tmp_arr[ac++] = String.fromCharCode(o1, o2);
        } else {
            tmp_arr[ac++] = String.fromCharCode(o1, o2, o3);
        }
    } while (i < data.length);

    dec = tmp_arr.join('');
    dec = this.utf8_decode(dec);

    return dec;
}

function base64_encode (data) {
    // Encodes string using MIME base64 algorithm  
    // 
    // version: 1103.1210
    // discuss at: http://phpjs.org/functions/base64_encode
    // +   original by: Tyler Akins (http://rumkin.com)
    // +   improved by: Bayron Guevara
    // +   improved by: Thunder.m
    // +   improved by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
    // +   bugfixed by: Pellentesque Malesuada
    // +   improved by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
    // -    depends on: utf8_encode
    // *     example 1: base64_encode('Kevin van Zonneveld');
    // *     returns 1: 'S2V2aW4gdmFuIFpvbm5ldmVsZA=='
    // mozilla has this native
    // - but breaks in 2.0.0.12!
    //if (typeof this.window['atob'] == 'function') {
    //    return atob(data);
    //}
    var b64 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
    var o1, o2, o3, h1, h2, h3, h4, bits, i = 0,
        ac = 0,
        enc = "",
        tmp_arr = [];

    if (!data) {
        return data;
    }

    data = this.utf8_encode(data + '');

    do { // pack three octets into four hexets
        o1 = data.charCodeAt(i++);
        o2 = data.charCodeAt(i++);
        o3 = data.charCodeAt(i++);

        bits = o1 << 16 | o2 << 8 | o3;

        h1 = bits >> 18 & 0x3f;
        h2 = bits >> 12 & 0x3f;
        h3 = bits >> 6 & 0x3f;
        h4 = bits & 0x3f;

        // use hexets to index into b64, and append result to encoded string
        tmp_arr[ac++] = b64.charAt(h1) + b64.charAt(h2) + b64.charAt(h3) + b64.charAt(h4);
    } while (i < data.length);

    enc = tmp_arr.join('');

    switch (data.length % 3) {
    case 1:
        enc = enc.slice(0, -2) + '==';
        break;
    case 2:
        enc = enc.slice(0, -1) + '=';
        break;
    }

    return enc;
}

function utf8_decode (str_data) {
    // Converts a UTF-8 encoded string to ISO-8859-1  
    // 
    // version: 1103.1210
    // discuss at: http://phpjs.org/functions/utf8_decode
    // +   original by: Webtoolkit.info (http://www.webtoolkit.info/)
    // +      input by: Aman Gupta
    // +   improved by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
    // +   improved by: Norman "zEh" Fuchs
    // +   bugfixed by: hitwork
    // +   bugfixed by: Onno Marsman
    // +      input by: Brett Zamir (http://brett-zamir.me)
    // +   bugfixed by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
    // *     example 1: utf8_decode('Kevin van Zonneveld');
    // *     returns 1: 'Kevin van Zonneveld'
    var tmp_arr = [],
        i = 0,
        ac = 0,
        c1 = 0,
        c2 = 0,
        c3 = 0;

    str_data += '';

    while (i < str_data.length) {
        c1 = str_data.charCodeAt(i);
        if (c1 < 128) {
            tmp_arr[ac++] = String.fromCharCode(c1);
            i++;
        } else if (c1 > 191 && c1 < 224) {
            c2 = str_data.charCodeAt(i + 1);
            tmp_arr[ac++] = String.fromCharCode(((c1 & 31) << 6) | (c2 & 63));
            i += 2;
        } else {
            c2 = str_data.charCodeAt(i + 1);
            c3 = str_data.charCodeAt(i + 2);
            tmp_arr[ac++] = String.fromCharCode(((c1 & 15) << 12) | ((c2 & 63) << 6) | (c3 & 63));
            i += 3;
        }
    }

    return tmp_arr.join('');
}

function utf8_encode (argString) {
    // Encodes an ISO-8859-1 string to UTF-8  
    // 
    // version: 1103.1210
    // discuss at: http://phpjs.org/functions/utf8_encode
    // +   original by: Webtoolkit.info (http://www.webtoolkit.info/)
    // +   improved by: Kevin van Zonneveld (http://kevin.vanzonneveld.net)
    // +   improved by: sowberry
    // +    tweaked by: Jack
    // +   bugfixed by: Onno Marsman
    // +   improved by: Yves Sucaet
    // +   bugfixed by: Onno Marsman
    // +   bugfixed by: Ulrich
    // *     example 1: utf8_encode('Kevin van Zonneveld');
    // *     returns 1: 'Kevin van Zonneveld'
    var string = (argString + ''); // .replace(/\r\n/g, "\n").replace(/\r/g, "\n");
    var utftext = "",
        start, end, stringl = 0;

    start = end = 0;
    stringl = string.length;
    for (var n = 0; n < stringl; n++) {
        var c1 = string.charCodeAt(n);
        var enc = null;

        if (c1 < 128) {
            end++;
        } else if (c1 > 127 && c1 < 2048) {
            enc = String.fromCharCode((c1 >> 6) | 192) + String.fromCharCode((c1 & 63) | 128);
        } else {
            enc = String.fromCharCode((c1 >> 12) | 224) + String.fromCharCode(((c1 >> 6) & 63) | 128) + String.fromCharCode((c1 & 63) | 128);
        }
        if (enc !== null) {
            if (end > start) {
                utftext += string.slice(start, end);
            }
            utftext += enc;
            start = end = n + 1;
        }
    }

    if (end > start) {
        utftext += string.slice(start, stringl);
    }

    return utftext;
}
