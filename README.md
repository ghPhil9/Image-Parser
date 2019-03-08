# Image Parser
YouTube video: https://youtu.be/Ni1FJHS2I2I

Parameters for start from console:

USAGE: [amount_images] [count_threads] [ip_proxy] [port_proxy]

EXAMPLE:

         "Image Parser" -img 100 --t 10 -ipp 192.168.0.0 --pp 443
         
         "Image Parser" -img 50 --t 10 // not necessarily using proxy
         
         "Image Parser" -img ? --t 20 // symbol "?" it is random from 0 to 2147483647 - similarity infinite loop

I recommend usage proxy if you need very lot pictures in order to avoid IP-ban. And dont use lot threads. They while not stable.

The application is analog prntscrScraper, but I want to expand and get better its functionality.
