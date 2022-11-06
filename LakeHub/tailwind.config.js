/** @type {import('tailwindcss').Config} */
const defaultTheme = require('tailwindcss/defaultTheme')
module.exports = {
    content: [
        "./wwwroot/**/*.{html,js}",
        "./Pages/**/*.cshtml",
        "./Views/**/*.cshtml",
        "./Areas/**/*.cshtml"
    ],
    theme: {
        extend: {
            fontFamily: {
                sans: ['Inter var', ...defaultTheme.fontFamily.sans]
            }
        },
    },
    plugins: [],
}
