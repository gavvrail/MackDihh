// Google Maps Configuration
const MAP_CONFIG = {
    // Tunku Abdul Rahman University Of Management And Technology, Sabah Branch
    latitude: 6.0285321,
    longitude: 116.1294082, // Using the precise pin location from the URL
    zoom: 17,
    address: "Tunku Abdul Rahman University Of Management And Technology, Sabah Branch, Jalan Donggongon, 89500 Penampang, Sabah, Malaysia",
};

// Function to generate Google Maps embed URL
function generateMapUrl() {
    const { latitude, longitude, zoom } = MAP_CONFIG;
    // This is a simplified and more reliable embed URL structure
    return `https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3969.999!2d${longitude}!3d${latitude}!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x323b665bce325bf1%3A0xfc7bcf77c145bc5f!2s${encodeURIComponent(MAP_CONFIG.address)}!5e0!3m2!1sen!2smy`;
}

// Function to generate Google Maps directions URL
function generateDirectionsUrl() {
    return `https://maps.google.com/maps?q=${encodeURIComponent(MAP_CONFIG.address)}`;
}

// Export for use in other files
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { MAP_CONFIG, generateMapUrl, generateDirectionsUrl };
}
