const path = require('path');
const CssMinimizerPlugin = require('css-minimizer-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');
const TerserWebpackPlugin = require( 'terser-webpack-plugin' );

module.exports = (env, options) => {
    const isProduction = options.mode === 'production';
    return {
        entry: {
            upload: './src/index.js',
        },
        output: {
            //path: path.resolve(__dirname, '..', 'Files'),
            path: path.resolve(__dirname, '..', '..', 'WebApp', 'wwwroot', 'cmpnt'),
            publicPath: './',
            filename: isProduction? 'upload.min.js' : 'upload.js',
            //chunkFilename: '[name].chunk.js'
        },
        module: {
            rules: [
                {
                    test: /\.css$/i,
                    use: [MiniCssExtractPlugin.loader, 'css-loader'],
                },
                {
                    test: /\.(scss)$/,
                    use: [
                        MiniCssExtractPlugin.loader,
                        {
                            loader: 'css-loader', // translates CSS into CommonJS modules
                        },
                        {
                            loader: 'postcss-loader' // Run postcss actions
                        },
                        {
                            loader: 'sass-loader' // compiles Sass to CSS
                        }]
                },
                {
                    test: /\.(woff|woff2|eot|ttf|otf)$/i,
                    type: "asset/resource",
                    generator: {
                        filename: '[name][ext][query]',
                    }
                },
                {
                    test: /\.(png|jpe?g|gif|ico|svg)(\?v=\d+\.\d+\.\d+)?$/i,
                    type: "asset/resource",
                    generator: {
                        filename: '[name][ext]'
                    }
                }
            ]
        },
        optimization: {
            //usedExports: true,
            minimizer: [
                new CssMinimizerPlugin({
                    parallel: true
                }),
                new TerserWebpackPlugin({
                    //test: /\.js(\?.*)?$/i,
                    terserOptions: {
                        sourceMap: true,
                        output: {
                            // Preserve license comments.
                            comments: /^!/
                        }
                    },
                    extractComments: false
                })
            ],
        },
        devtool: 'source-map',
        plugins: [
            new MiniCssExtractPlugin({
                filename: isProduction? 'upload.min.css' : 'upload.css',
            })
        ]
    }
}