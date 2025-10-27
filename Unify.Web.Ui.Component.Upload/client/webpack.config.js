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
            path: path.resolve(__dirname, '..', 'Files'),
            publicPath: './',
            filename: isProduction? 'upload.min.js' : 'upload.js'
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
                            loader: 'css-loader',
                        },
                        {
                            loader: 'postcss-loader'
                        },
                        {
                            loader: 'sass-loader'
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
            minimizer: [
                new CssMinimizerPlugin({
                    parallel: true
                }),
                new TerserWebpackPlugin({
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